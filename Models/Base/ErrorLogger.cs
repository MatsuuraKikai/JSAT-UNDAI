using System.Globalization;
using System.IO;
using System.Text;

namespace JSAT_UNDAI.Models.Base;

public sealed class ErrorLogger : IDisposable
{
    private const string FilePrefix = "MessageLog_";
    private const string FileDateFormat = "yyyy-MM-dd";
    private const string MonthFolderFormat = "yyyy-MM";

    private FileStream? _stream;
    private StreamWriter? _writer;

    private readonly object _lock = new();

    private string? _logDirectory;

    private int _currentDayKey;

    public void Start(string logDirectory)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(logDirectory);
            _logDirectory = logDirectory;

            try
            {
                OpenWriterForToday_NoLock(DateTime.Now);
            }
            catch
            {
                _logDirectory = null;
                throw;
            }
        }
    }

    public void Log(string message, bool flushToDisk = false)
    {
        lock (_lock)
        {
            if (_logDirectory is null)
            {
                return;
            }

            var now = DateTime.Now;

            if (DayKey(now) != _currentDayKey || _writer is null)
            {
                OpenWriterForToday_NoLock(now);
            }

            _writer?.WriteLine(message);

            if (flushToDisk)
            {
                _stream?.Flush(true);
            }
        }
    }

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

    private void OpenWriterForToday_NoLock(DateTime now)
    {
        CloseWriter_NoLock();

        var monthDirectory = Path.Combine(_logDirectory!, now.ToString(MonthFolderFormat, CultureInfo.InvariantCulture));
        Directory.CreateDirectory(monthDirectory);

        var fileName = $"{FilePrefix}{now.ToString(FileDateFormat, CultureInfo.InvariantCulture)}.txt";
        var path = Path.Combine(monthDirectory, fileName);

        _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

        _currentDayKey = DayKey(now);
    }

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

    private static int DayKey(DateTime dt)
    {
        return (dt.Year * 10000) + (dt.Month * 100) + dt.Day;
    }
}
