using System;

namespace Ldv.Scrappy.Bll
{
    /// <summary>
    /// https://stackoverflow.com/questions/5646820/logger-wrapper-best-practice
    /// </summary>
    public static class ILoggerExtensions
    {
        public static void Log(this ILogger logger, string message)
        {
            logger.Log(new LogEntry(LoggingEventType.Information, message));
        }

        public static void Log(this ILogger logger, Exception exception)
        {
            logger.Log(new LogEntry(LoggingEventType.Error, exception.Message, exception));
        }
    }
}