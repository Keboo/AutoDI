using System.Runtime.CompilerServices;

namespace AutoDI.Build
{
    public interface ILogger
    {
        bool ErrorLogged { get; }
        DebugLogLevel DebugLogLevel { get; set; }

        void Debug(string message, DebugLogLevel debugLevel);
        void Info(string message);
        void Warning(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0);
        void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0);
    }
}