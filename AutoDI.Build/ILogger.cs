namespace AutoDI.Build
{
    public interface ILogger
    {
        bool ErrorLogged { get; }
        DebugLogLevel DebugLogLevel { get; set; }

        void Debug(string message, DebugLogLevel debugLevel);
        void Info(string message);
        void Warning(string message, AdditionalInformation additionalInformation = null);
        //This could probably just replace the AdditionalInformation class with SequencePoint
        void Error(string message, AdditionalInformation additionalInformation);
    }
}