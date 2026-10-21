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

            _cts = new CancellationTokenSource();
            _client = new TcpClient();

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            connectCts.CancelAfter(10000);

            await _client.ConnectAsync(endpoint.Address, endpoint.Port, connectCts.Token);

            IsConnected = true;
            ConnectionStatusChanged?.Invoke(true);

            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
            _ = Task.Run(() => ConnectionHealthLoopAsync(_cts.Token));
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
            _cts?.Cancel();
            CleanupAfterDisconnect();
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error disconnecting from TCP server.", ex);
        }
    }

    private void CleanupAfterDisconnect()
    {
        bool wasConnected = IsConnected;

        try { _client?.Close(); } catch { /* already closed */ }
        _client = null;

        _cts?.Dispose();
        _cts = null;

        IsConnected = false;
        _messageBuffer.Clear();

        if (wasConnected)
        {
            ConnectionStatusChanged?.Invoke(false);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        if (_client == null) return;

        try
        {
            var stream = _client.GetStream();
            var buffer = new byte[8192];

            while (!token.IsCancellationRequested && _client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, token);
                if (bytesRead == 0)
                {
                    break; // remote closed gracefully
                }

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                _messageBuffer.Append(chunk);

                foreach (var line in ExtractCompleteLines())
                {
                    MessageReceived?.Invoke(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on manual disconnect
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError("Error receiving TCP data.", ex);
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                // stream ended unexpectedly -> treat as disconnect
                CleanupAfterDisconnect();
            }
        }
    }

    /// <summary>
    /// Splits the internal buffer on '\n', returns each complete line (no content
    /// filtering/parsing), and keeps any trailing partial line for the next read.
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

    private async Task ConnectionHealthLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, token);

                if (_client == null || !IsSocketStillConnected(_client.Client))
                {
                    _ = ErrorLoggingService.LogError("TCP connection lost (health check).");
                    CleanupAfterDisconnect();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _ = ErrorLoggingService.LogError("Error in TCP connection health check.", ex);
                CleanupAfterDisconnect();
                return;
            }
        }
    }

    /// <summary>
    /// TcpClient.Connected only reflects the last I/O operation, not real-time state.
    /// Poll + Available==0 is the standard way to detect a half-closed socket. [Confidence: High]
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
