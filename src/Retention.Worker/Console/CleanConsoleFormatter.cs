using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Retention.Worker.Console
{
    /// <summary>
    /// A simple console formatter for demonstrative purposes.
    /// This formatter outputs log messages in a clean, easy-to-read format,
    /// making logs more readable during development or demonstrations.
    /// </summary>
    public class CleanConsoleFormatter : ConsoleFormatter
    {
        public CleanConsoleFormatter() : base("clean") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            var level = GetLogLevelString(logEntry.LogLevel);
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            textWriter.WriteLine($"{level}: {message}");
        }

        private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "INFO"
        };
    }
}
