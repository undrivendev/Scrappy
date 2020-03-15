using Ldv.Scrappy.Bll;
using Serilog.Events;

namespace Ldv.Scrappy.Downloader.ConsoleApp
{
    public class SerilogILoggerWrapper : ILogger
    {
        private readonly Serilog.ILogger _logger;

        public SerilogILoggerWrapper(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Log(LogEntry entry) 
            => _logger.Write((LogEventLevel)entry.Severity, entry.Exception, entry.Message);
    }
}