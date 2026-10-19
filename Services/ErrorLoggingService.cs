using JSAT_UNDAI.Models.Base;
using System.Runtime.CompilerServices;

namespace JSAT_UNDAI.Services;

public static class ErrorLoggingService
{
    /// <summary>
    /// Defines the _errorLogger
    /// </summary>
    private static readonly ErrorLogger _errorLogger = new();

    /// <summary>
    /// Initializes the error logging service.
    /// </summary>
    /// <param name="logDirectory">Directory to write ErrorLog_yyyy-MM.txt files into.</param>
    public static void Initialize(string logDirectory)
    {
        try
        {
            _errorLogger.Start(logDirectory);
        }
        catch
        {
            // Logging initialization must never crash the application.
        }
    }

    /// <summary>
    /// Logs informational messages.
    /// </summary>
    public static Task LogInfo(
        string message,
        [CallerMemberName] string caller = "")
    {
        return Log(
            "INFO",
            message,
            null,
            caller);
    }

    /// <summary>
    /// Logs warning messages.
    /// </summary>
    public static Task LogWarning(
        string message,
        Exception? exception = null,
        [CallerMemberName] string caller = "")
    {
        return Log(
            "WARN",
            message,
            exception,
            caller);
    }

    /// <summary>
    /// Logs error messages.
    /// </summary>
    public static Task LogError(
        string message,
        Exception? exception = null,
        [CallerMemberName] string caller = "")
    {
        return Log(
            "ERROR",
            message,
            exception,
            caller);
    }

    /// <summary>
    /// Internal logging implementation.
    /// </summary>
    private static Task Log(
        string level,
        string message,
        Exception? exception,
        string caller)
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

            _errorLogger.Log(line);
        }
        catch
        {
            // Logging must never crash the application.
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the error logging service.
    /// </summary>
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
