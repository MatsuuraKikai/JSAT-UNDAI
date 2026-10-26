using JSAT_UNDAI.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace JSAT_UNDAI.Services;

public class TcpServerConnectionService : IDisposable
{
    private readonly UndaiSettingStorageService _undaiSettingStorageService;
    //TcpTrafficLogService tcpLog;

    IPEndPoint endpoint;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private readonly StringBuilder _messageBuffer = new();

    private readonly object _bufferLock = new();
    private readonly object _cleanupLock = new();

    public bool IsConnected { get; private set; }
    public string CurrentServerIP { get; private set; } = string.Empty;
    public int CurrentServerPort { get; private set; } = 5625; // default port

    private static readonly string[] Prefixes = { "SY,", "A,", "SI,", "SA," };

    /// <summary>
    /// Raised whenever the connection state flips (true = connected).
    /// </summary>
    public event Action<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Raised once per complete line received from the server.
    /// No semantic parsing is done here — just newline-delimited framing.
    /// Consume and interpret the payload downstream.
    /// </summary>
    public event Action<string>? MessageReceived;

    // Connection health tracking
    private DateTime _lastMessageTime = DateTime.UtcNow;
    CancellationTokenSource _watchdogTimerCts = new();
    int? _lastRandomNumber = null;
    int _sameCount = 0;
    const int MaxSameCount = 3;

    public TcpServerConnectionService(string appDataDirectory)
    {
        _undaiSettingStorageService = new UndaiSettingStorageService(appDataDirectory);
    }

    public async Task ConnectAsync()
    {
        if (IsConnected || _client != null)
        {
            return;
        }

        try
        {
            var config = _undaiSettingStorageService.LoadSetting();
            CurrentServerIP = config.CurrentUndaiIpAddress;
            CurrentServerPort = UndaiSettingEntry.DefaultUndaiPort;

            endpoint = new IPEndPoint(IPAddress.Parse(CurrentServerIP), CurrentServerPort);

            var cts = new CancellationTokenSource();
            var client = new TcpClient();
            _cts = cts;
            _client = client;

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            connectCts.CancelAfter(10000);

            await client.ConnectAsync(endpoint.Address, endpoint.Port, connectCts.Token);

            IsConnected = true;
            ConnectionStatusChanged?.Invoke(true);

            var token = cts.Token;
            _ = Task.Run(() => ReceiveLoopAsync(client, token));

            StartWatchdogTimer();

            App.TcpLog.LogEvent(TcpLogType.Connect, endpoint.ToString(), "Connected");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError(
                $"Error connecting to TCP server. IP={CurrentServerIP}, Port={CurrentServerPort}", ex);

            CleanupAfterDisconnect();
        }
    }

    public void Disconnect()
    {
        try
        {
            CleanupAfterDisconnect(); // cancels, closes, and disposes in the right order
            StopWatchdogTimer();

            App.TcpLog.LogEvent(TcpLogType.Disconnect, endpoint.ToString(), "Disconnected");
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error disconnecting from TCP server.", ex);
        }
    }

    private void CleanupAfterDisconnect()
    {
        bool wasConnected;

        lock (_cleanupLock)
        {
            wasConnected = IsConnected;

            var cts = _cts;
            var client = _client;
            _cts = null;
            _client = null;
            IsConnected = false;

            try { cts?.Cancel(); } catch { /* already disposed */ }
            try { client?.Close(); } catch { /* already closed */ }
            try { cts?.Dispose(); } catch { }

            lock (_bufferLock)
            {
                _messageBuffer.Clear();
            }
        }

        // Raise outside the lock so a subscriber calling back into the service can't deadlock
        if (wasConnected)
        {
            ConnectionStatusChanged?.Invoke(false);
        }
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[256];

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, token);
                if (bytesRead == 0)
                {
                    break; // remote closed gracefully
                }

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                List<string> lines;
                lock (_bufferLock)
                {
                    _messageBuffer.Append(chunk);
                    lines = ExtractCompleteLines();
                }

                foreach (var line in lines)
                {
                    App.TcpLog.LogRx(endpoint.ToString(), line);
                    MessageReceived?.Invoke(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on manual disconnect
        }
        catch (Exception ex) when (ex is ObjectDisposedException or IOException)
        {
            // socket closed underneath us (disconnect / health check) - expected
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error receiving TCP data.", ex);
        }
        finally
        {
            // Only clean up if this loop's client is still the active one
            if (!token.IsCancellationRequested && ReferenceEquals(_client, client))
            {
                CleanupAfterDisconnect();
            }
        }
    }

    /// <summary>
    /// Splits the internal buffer on '\n', returns each complete line, and keeps
    /// any trailing partial line for the next read. Caller must hold _bufferLock.
    /// </summary>
    private List<string> ExtractCompleteLines()
    {
        var lines = new List<string>();
        string buffered = _messageBuffer.ToString().Replace("\0", "");

        int newlineIndex;
        int searchStart = 0;

        while ((newlineIndex = buffered.IndexOf('\n', searchStart)) != -1)
        {
            string line = buffered.Substring(searchStart, newlineIndex - searchStart).TrimEnd('\r');
            searchStart = newlineIndex + 1;

            int start = FindMessageStart(line);
            if (start >= 0)
            {
                lines.Add(line.Substring(start));
            }
        }

        _messageBuffer.Clear();
        _messageBuffer.Append(buffered.Substring(searchStart)); // keep leftover partial line

        return lines;
    }

    private int FindMessageStart(string line)
    {
        int best = -1;
        foreach (string prefix in Prefixes)
        {
            int idx = line.IndexOf(prefix, StringComparison.Ordinal);
            if (idx >= 0 && (best == -1 || idx < best))
            {
                best = idx;
            }
        }
        return best;
    }

    public void Dispose()
    {
        Disconnect();
    }

    void StartWatchdogTimer()
    {
        _lastMessageTime = DateTime.UtcNow;
        _watchdogTimerCts = new CancellationTokenSource();
        _ = ConnectionWatchdog(_watchdogTimerCts.Token);
    }

    void StopWatchdogTimer()
    {
        _watchdogTimerCts?.Cancel();
        _watchdogTimerCts?.Dispose();
        _watchdogTimerCts = null;
    }

    public bool IsConnectionStale(int randomNumber)
    {
        _lastMessageTime = DateTime.UtcNow;
        if(_lastRandomNumber == randomNumber)
        {
            _sameCount++;
        }
        else
        {
            _lastRandomNumber = randomNumber;
            _sameCount = 0;
        }

        return _sameCount >= MaxSameCount;
    }

    async Task ConnectionWatchdog(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (DateTime.UtcNow - _lastMessageTime > TimeSpan.FromSeconds(5))
                {
                    _ = ErrorLoggingService.LogError("TCP connection is stale. Disconnecting.");
                    Disconnect();
                    break;
                }
                await Task.Delay(1000, token); // check every second
            }

        }
        catch (OperationCanceledException)
        {
            // expected on manual disconnect
        }

    }

    public async void SendMessage(string message)
    {
        if (!IsConnected || _client == null)
        {
            _ = ErrorLoggingService.LogError("Cannot send message. Not connected to TCP server.");
            return;
        }
        try
        {
            var stream = _client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message + ",\n");
            await stream.WriteAsync(data, 0, data.Length);
            App.TcpLog.LogTx(endpoint.ToString(), message);
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error sending TCP message.", ex);
        }
    }
}
