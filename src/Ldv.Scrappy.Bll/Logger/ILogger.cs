namespace Ldv.Scrappy.Bll
{
    /// <summary>
    /// https://stackoverflow.com/questions/5646820/logger-wrapper-best-practice
    /// </summary>
    public interface ILogger
    {
        void Log(LogEntry entry);
    }
}