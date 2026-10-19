using System.IO;

namespace JSAT_UNDAI.Models.Base;

/// <summary>
/// Defines the <see cref="ErrorLogger" />
/// Writes to ErrorLog_yyyy-MM.txt under the given directory, rotating to a
/// new file automatically the first time Log() is called after the month
/// changes (relevant if the app is left running across a month boundary).
/// </summary>
public sealed class ErrorLogger : IDisposable
{
    /// <summary>
    /// Defines the _writer
    /// </summary>
    private StreamWriter? _writer;

    /// <summary>
    /// Guards <see cref="_writer"/> and the rotation check - Log() can be
    /// called from different threads (UI thread, task scheduler, AppDomain
    /// unhandled-exception handlers), and StreamWriter is not thread-safe.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Directory the monthly log files are written into.
    /// </summary>
    private string? _logDirectory;

    /// <summary>
    /// Year/month of the file <see cref="_writer"/> currently points at,
    /// e.g. 202607 for July 2026. Compared against DateTime.Now on every
    /// Log() call to detect a month rollover.
    /// </summary>
    private int _currentMonthKey;

    /// <summary>
    /// The Start
    /// </summary>
    /// <param name="logDirectory">Directory to write ErrorLog_yyyy-MM.txt files into<see cref="string"/></param>
    public void Start(string logDirectory)
    {
        lock (_lock)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);

            OpenWriterForCurrentMonth_NoLock();
        }
    }

    /// <summary>
    /// The Log
    /// </summary>
    /// <param name="message">The message<see cref="string"/></param>
    public void Log(string message)
    {
        lock (_lock)
        {
            if (_logDirectory is null)
            {
                return;
            }

            int nowKey = MonthKey(DateTime.Now);
            if (nowKey != _currentMonthKey)
            {
                OpenWriterForCurrentMonth_NoLock();
            }

            _writer?.WriteLine(message);
        }
    }

    /// <summary>
    /// The Stop
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    /// <summary>
    /// The Dispose
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Closes the current writer (if any) and opens ErrorLog_yyyy-MM.txt for
    /// the current month. Caller must hold <see cref="_lock"/>.
    /// </summary>
    private void OpenWriterForCurrentMonth_NoLock()
    {
        var now = DateTime.Now;

        _writer?.Dispose();

        var fileName = $"ErrorLog_{now:yyyy-MM}.txt";
        var path = Path.Combine(_logDirectory!, fileName);

        _writer = new StreamWriter(path, append: true)
        {
            AutoFlush = true
        };

        _currentMonthKey = MonthKey(now);
    }

    private static int MonthKey(DateTime dt) => (dt.Year * 100) + dt.Month;
}
