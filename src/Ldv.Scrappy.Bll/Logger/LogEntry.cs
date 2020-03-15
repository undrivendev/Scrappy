using System;

namespace Ldv.Scrappy.Bll
{
    /// <summary>
    /// https://stackoverflow.com/questions/5646820/logger-wrapper-best-practice
    /// </summary>
    public class LogEntry
    {
        public LoggingEventType Severity { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public LogEntry(LoggingEventType severity, string message, Exception exception = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message == string.Empty) throw new ArgumentException("empty", nameof(message));

            this.Severity = severity;
            this.Message = message;
            this.Exception = exception;
        }
    }
}