using System.IO;
using System.Text;

namespace JSAT_UNDAI.Models.Base;

/// <summary>
/// Writes to ErrorLog_yyyy-MM.txt under the given directory, rotating to a
/// new file automatically the first time Log() is called after the month
/// changes (relevant if the app is left running across a month boundary).
/// Keeps only the newest <see cref="MaxLogFilesToKeep"/> monthly files.
/// </summary>
public sealed class ErrorLogger : IDisposable
{
    private const string FilePrefix = "ErrorLog_";
    private const string FileSearchPattern = "ErrorLog_*.txt";
    private const int MaxLogFilesToKeep = 12;

    private FileStream? _stream;
    private StreamWriter? _writer;

    /// <summary>
    /// Guards the stream/writer and the rotation check - Log() can be
    /// called from different threads (UI thread, task scheduler, AppDomain
    /// unhandled-exception handlers), and StreamWriter is not thread-safe.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Directory the monthly log files are written into. Null = logging off
    /// (not started, failed to start, or stopped).
    /// </summary>
    private string? _logDirectory;

    /// <summary>
    /// Year/month of the file currently open, e.g. 202607 for July 2026.
    /// Compared against DateTime.Now on every Log() call to detect a month rollover.
    /// </summary>
    private int _currentMonthKey;

    /// <summary>
    /// Opens this month's log file and removes old monthly files.
    /// Throws if the log file cannot be opened; logging then stays off.
    /// </summary>
    /// <param name="logDirectory">Directory to write ErrorLog_yyyy-MM.txt files into.</param>
    public void Start(string logDirectory)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(logDirectory);
            _logDirectory = logDirectory;

            try
            {
                OpenWriterForCurrentMonth_NoLock();
            }
            catch
            {
                _logDirectory = null;
                throw;
            }

            DeleteOldLogs_NoLock();
        }
    }

    /// <summary>
    /// Writes one line.
    /// </summary>
    /// <param name="message">The text to write.</param>
    /// <param name="flushToDisk">
    /// true = force the data to physical disk (survives a sudden power-off).
    /// Slower, so use it for WARN/ERROR only. false = flushed to the OS cache only.
    /// </param>
    public void Log(string message, bool flushToDisk = false)
    {
        lock (_lock)
        {
            if (_logDirectory is null)
            {
                return;
            }

            // Reopen on month rollover, or if a previous open attempt failed.
            if (MonthKey(DateTime.Now) != _currentMonthKey || _writer is null)
            {
                OpenWriterForCurrentMonth_NoLock();
            }

            _writer?.WriteLine(message);

            if (flushToDisk)
            {
                _stream?.Flush(true);
            }
        }
    }

    /// <summary>
    /// Closes the log file. Logging stays off until Start() is called again.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            CloseWriter_NoLock();
            _logDirectory = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Closes the current writer (if any) and opens ErrorLog_yyyy-MM.txt for
    /// the current month. Caller must hold <see cref="_lock"/>.
    /// If opening fails, no writer is left open and the exception propagates;
    /// the next Log() call retries.
    /// </summary>
    private void OpenWriterForCurrentMonth_NoLock()
    {
        var now = DateTime.Now;

        CloseWriter_NoLock();

        var path = Path.Combine(_logDirectory!, $"{FilePrefix}{now:yyyy-MM}.txt");

        // FileShare.ReadWrite: other tools (editors, log viewers) can open the
        // file while the app is running without breaking the logger.
        _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

        _currentMonthKey = MonthKey(now);
    }

    /// <summary>
    /// Disposes the writer and stream and nulls both, so a failed reopen can
    /// never leave a disposed writer behind. Caller must hold <see cref="_lock"/>.
    /// </summary>
    private void CloseWriter_NoLock()
    {
        try
        {
            _writer?.Dispose();
            _stream?.Dispose();
        }
        catch
        {
            // Closing must never throw (e.g. disk full on final flush).
        }
        finally
        {
            _writer = null;
            _stream = null;
        }
    }

    /// <summary>
    /// Deletes all but the newest <see cref="MaxLogFilesToKeep"/> monthly log files.
    /// File names sort chronologically (yyyy-MM). Caller must hold <see cref="_lock"/>.
    /// </summary>
    private void DeleteOldLogs_NoLock()
    {
        try
        {
            var oldFiles = Directory
                .EnumerateFiles(_logDirectory!, FileSearchPattern)
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .Skip(MaxLogFilesToKeep);

            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Locked or read-only: skip, try again next start.
                }
            }
        }
        catch
        {
            // Retention is best-effort and must never block logging.
        }
    }

    private static int MonthKey(DateTime dt) => (dt.Year * 100) + dt.Month;
}