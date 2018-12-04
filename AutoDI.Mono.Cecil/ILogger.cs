namespace AutoDI.Mono.Cecil
{
    public interface ILogger
    {
        void Debug(string message, DebugLogLevel debugLevel);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}