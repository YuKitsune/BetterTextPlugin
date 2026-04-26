using System.Runtime.ExceptionServices;
using Serilog;
using vatsys;

namespace BetterTextPlugin;

public interface IErrorReporter
{
    void ReportError(Exception exception);
}

public class ErrorReporter(string source) : IErrorReporter
{
    static readonly Dictionary<string, DateTimeOffset> ErrorMessages = new();

    public void ReportError(Exception exception)
    {
        TryAddErrorInternal(exception);
    }

    void TryAddErrorInternal(Exception exception)
    {
        lock (ErrorMessages)
        {
            var now = DateTime.UtcNow;

            // Don't flood the error window with the same message over and over again
            if (ErrorMessages.TryGetValue(exception.Message, out var lastShown) &&
                now - lastShown <= TimeSpan.FromMinutes(1))
                return;

            Errors.Add(exception, source);
            ErrorMessages[exception.Message] = now;
        }
    }
}

public static class ErrorReporterExtensions
{
    public static void ReportError(this IErrorReporter errorReporter, Exception exception, string message)
    {
        var newException = new Exception(message, exception);
        errorReporter.ReportError(newException);
    }
}
