using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JSAT_UNDAI.Services;

public class TcpServerConnectionService : IDisposable
{
    private readonly UndaiSettingStorageService _undaiSettingStorageService;

    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private readonly StringBuilder _messageBuffer = new();

    private readonly object _bufferLock = new();
    private readonly object _cleanupLock = new();

    public bool IsConnected { get; private set; }
    public string CurrentServerIP { get; private set; } = string.Empty;
    public int CurrentServerPort { get; private set; } = 5625; // default port

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

            var endpoint = new IPEndPoint(IPAddress.Parse(CurrentServerIP), CurrentServerPort);

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
            _ = Task.Run(() => ConnectionHealthLoopAsync(client, token));
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
            var buffer = new byte[8192];

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
        string buffered = _messageBuffer.ToString();

        int newlineIndex;
        int searchStart = 0;

        while ((newlineIndex = buffered.IndexOf('\n', searchStart)) != -1)
        {
            string line = buffered.Substring(searchStart, newlineIndex - searchStart).TrimEnd('\r');
            if (line.Length > 0)
            {
                lines.Add(line);
            }
            searchStart = newlineIndex + 1;
        }

        _messageBuffer.Clear();
        _messageBuffer.Append(buffered.Substring(searchStart)); // keep leftover partial line

        return lines;
    }

    private async Task ConnectionHealthLoopAsync(TcpClient client, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, token);

                if (!IsSocketStillConnected(client.Client))
                {
                    // Skip if this connection was already replaced or torn down
                    if (!token.IsCancellationRequested && ReferenceEquals(_client, client))
                    {
                        _ = ErrorLoggingService.LogError("TCP connection lost (health check).");
                        CleanupAfterDisconnect();
                    }
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested && ReferenceEquals(_client, client))
                {
                    _ = ErrorLoggingService.LogError("Error in TCP connection health check.", ex);
                    CleanupAfterDisconnect();
                }
                return;
            }
        }
    }

    /// <summary>
    /// TcpClient.Connected only reflects the last I/O operation, not real-time state.
    /// Poll + Available==0 is the standard way to detect a half-closed socket.
    /// </summary>
    private static bool IsSocketStillConnected(Socket socket)
    {
        try
        {
            bool readable = socket.Poll(0, SelectMode.SelectRead);
            bool noData = socket.Available == 0;
            return !(readable && noData);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
