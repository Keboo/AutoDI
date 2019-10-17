namespace AutoDI.Build
{
    public interface ILogger
    {
        bool ErrorLogged { get; }
        DebugLogLevel DebugLogLevel { get; set; }

        void Debug(string message, DebugLogLevel debugLevel);
        void Info(string message);
        void Warning(string message,AdditionalInformation additionalInformation=null);
        void Error(string message,AdditionalInformation additionalInformation);
    }
}