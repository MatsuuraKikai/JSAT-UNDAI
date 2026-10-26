using JSAT_UNDAI.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace JSAT_UNDAI.Services;

public sealed class TcpTrafficLogService : IAsyncDisposable, IDisposable
{
    private const int ChannelCapacity = 20_000;          // max queued entries before dropping
    private const long MaxFileBytes = 20L * 1024 * 1024; // roll at 20 MB
    private const int RetentionDays = 14;                // delete log files older than this
    private const int MaxDataChars = 4096;               // protects against garbage / runaway frames
    private const string FilePrefix = "TcpLog_";
    private const string FileExtension = ".jsonl";

    private readonly string _logDirectory;
    private readonly Channel<TcpLogEntry> _channel;
    private readonly Task _writerTask;
    private readonly JsonSerializerOptions _jsonOptions;

    private long _droppedCount;   // incremented by producers, drained by the writer
    private int _disposed;

    // --- State below is touched ONLY by the writer task (no locking needed) ---
    private StreamWriter? _writer;
    private DateOnly _currentDate;
    private int _currentPart;
    private long _currentBytes;

    public string LogDirectory => _logDirectory;

    public TcpTrafficLogService(string appDataDirectory)
    {
        _logDirectory = appDataDirectory;
        Directory.CreateDirectory(_logDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false, // MUST be false: one entry = one line
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep '$' ',' etc. readable
            Converters = { new JsonStringEnumConverter() }
        };

        // Wait mode + TryWrite: TryWrite returns false when full, so we can count drops.
        _channel = Channel.CreateBounded<TcpLogEntry>(new BoundedChannelOptions(ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _writerTask = Task.Run(WriterLoop);
    }

    // ───────────────────────── Public API (thread-safe, non-blocking) ─────────────────────────
    
    public void LogRx(string endPoint, string data)
    {
        Enqueue(TcpLogType.RX, endPoint, data);
    }

    public void LogTx(string endPoint, string data)
    {
        Enqueue(TcpLogType.TX, endPoint, data);
    }

    public void LogEvent(TcpLogType type, string endpoint, string message)
    {
        Enqueue(type, endpoint, message);
    }

    private void Enqueue(TcpLogType type, string endPoint, string data)
    {
        if(Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        data ??= string.Empty;
        int originalLength = data.Length;

        if(data.Length > MaxDataChars)
        {
            data = string.Concat(data.AsSpan(0, MaxDataChars), "...[truncated]");
        }

        var entry = new TcpLogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Type = type,
            Endpoint = endPoint ?? string.Empty,
            Length = originalLength,
            Message = data
        };

        if (!_channel.Writer.TryWrite(entry))
        {
            Interlocked.Increment(ref _droppedCount);
        }
    }

    // ───────────────────────── Writer thread ─────────────────────────
    private async Task WriterLoop()
    {
        var reader = _channel.Reader;
        try
        {
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                // Drain everything currently queued, then flush once (batching).
                while (reader.TryRead(out var entry))
                {
                    WriteEntry(entry);
                }

                ReportDropped();
                FlushWriter();
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLoggingService.LogError($"TCP log writer loop crashed: {ex}");
        }
        finally
        {
            ReportDropped();
            CloseWriter(); // flushes
        }
    }

    private void WriteEntry(TcpLogEntry entry)
    {
        try
        {
            string line = JsonSerializer.Serialize(entry, _jsonOptions);

            EnsureWriter(entry.Timestamp.LocalDateTime);

            _writer!.WriteLine(line);
            _currentBytes += Encoding.UTF8.GetByteCount(line) + 1; // +1 for '\n'
        }
        catch (Exception ex)
        {
            // Disk full / file locked / path gone: drop this entry, reopen on the next one.
            Debug.WriteLine($"TCP log write failed: {ex.Message}");
            CloseWriter();
        }
    }

    private void ReportDropped()
    {
        long dropped = Interlocked.Exchange(ref _droppedCount, 0);
        if (dropped > 0)
        {
            WriteEntry(new TcpLogEntry
            {
                Timestamp = DateTimeOffset.Now,
                Type = TcpLogType.Error,
                Endpoint = string.Empty,
                Message = $"Log queue full: {dropped} entries dropped"
            });
        }
    }

    // ───────────────────────── File management ─────────────────────────

    private void EnsureWriter(DateTime localTime)
    {
        var date = DateOnly.FromDateTime(localTime);

        if (_writer != null)
        {
            bool dayChanged = date != _currentDate;
            bool tooBig = _currentBytes >= MaxFileBytes;

            if (!dayChanged && !tooBig) return;

            CloseWriter();
            _currentPart = dayChanged ? 0 : _currentPart + 1;
        }
        else if (date != _currentDate)
        {
            _currentPart = 0;
        }

        OpenWriter(date);
    }

    private void OpenWriter(DateOnly date)
    {
        // Skip parts that are already full (e.g. app restarted mid-day).
        string path = BuildPath(date, _currentPart);
        while (File.Exists(path) && new FileInfo(path).Length >= MaxFileBytes)
        {
            _currentPart++;
            path = BuildPath(date, _currentPart);
        }

        // FileShare.Read: you can tail/open the file while the app is still writing.
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 64 * 1024);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) // UTF-8, no BOM
        {
            NewLine = "\n",
            AutoFlush = false
        };

        _currentDate = date;
        _currentBytes = stream.Length;

        DeleteOldLogs();
    }

    private string BuildPath(DateOnly date, int part)
    {
        string name = string.Empty;
        if (part == 0)
        {
            name = $"{FilePrefix}{date:yyyyMMdd}{FileExtension}";
        }
        else
        {
            name = $"{FilePrefix}{date:yyyyMMdd}_{part:D3}{FileExtension}";
        }

        var monthDirectory = Path.Combine(_logDirectory!, date.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(monthDirectory);

        return Path.Combine(monthDirectory, name);
    }

    private void DeleteOldLogs()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (var file in Directory.EnumerateFiles(_logDirectory, $"{FilePrefix}*{FileExtension}"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TCP log cleanup failed: {ex.Message}");
        }
    }

    private void FlushWriter()
    {
        try
        {
            _writer?.Flush();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TCP log flush failed: {ex.Message}");
            CloseWriter();
        }
    }

    private void CloseWriter()
    {
        try
        {
            _writer?.Dispose(); // flushes then closes the underlying stream
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TCP log close failed: {ex.Message}");
        }
        finally
        {
            _writer = null;
        }
    }

    // ───────────────────────── Shutdown ─────────────────────────

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if(Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await Task.WhenAny(_writerTask, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
    }
}
