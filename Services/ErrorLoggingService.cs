using JSAT_UNDAI.Models.Base;
using System.Runtime.CompilerServices;

namespace JSAT_UNDAI.Services;

public static class ErrorLoggingService
{
    static App? _app;
    private static readonly ErrorLogger _errorLogger = new();

    public static void Initialize(App app, string logDirectory)
    {
        try
        {
            _app = app;
            _errorLogger.Start(logDirectory);
        }
        catch
        {
            // Logging initialization must never crash the application.
        }
    }

    public static Task LogInfo(
        string message,
        [CallerMemberName] string caller = "")
    {
        return Log(
            "INFO",
            message,
            null,
            caller,
            flushToDisk: false);
    }

    public static Task LogWarning(
        string message,
        Exception? exception = null,
        [CallerMemberName] string caller = "")
    {
        return Log(
            "WARN",
            message,
            exception,
            caller,
            flushToDisk: true);
    }

    public static Task LogError(
        string message,
        Exception? exception = null,
        [CallerMemberName] string caller = "")
    {
        if(_app is not null)
        {
            _ = _app._alarmPageViewModel.RaiseAlarmAsync(message);
        }

        return Log(
            "ERROR",
            message,
            exception,
            caller,
            flushToDisk: true);
    }

    private static Task Log(
        string level,
        string message,
        Exception? exception,
        string caller,
        bool flushToDisk)
    {
        try
        {
            string line;

            if (exception is null)
            {
                line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"[{level}] " +
                    $"[{caller}] " +
                    message;
            }
            else
            {
                line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"[{level}] " +
                    $"[{caller}] " +
                    $"{message}{Environment.NewLine}" +
                    $"Exception: {exception.GetType().Name}{Environment.NewLine}" +
                    $"Message: {exception.Message}{Environment.NewLine}" +
                    $"StackTrace:{Environment.NewLine}" +
                    $"{exception.StackTrace}";

                if (exception.InnerException != null)
                {
                    line +=
                        $"{Environment.NewLine}" +
                        $"InnerException: {exception.InnerException.Message}";
                }
            }

            _errorLogger.Log(line, flushToDisk);
        }
        catch
        {
            // Logging must never crash the application.
        }

        return Task.CompletedTask;
    }

    public static void Shutdown()
    {
        try
        {
            _errorLogger.Stop();
        }
        catch
        {
            // Shutdown logging failures are ignored.
        }
    }
}
