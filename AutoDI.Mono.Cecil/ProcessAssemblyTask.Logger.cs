using System;

namespace AutoDI.Mono.Cecil
{
    partial class ProcessAssemblyTask
    {
        private ILogger Logger { get; set; }

        private class WeaverLogger : ILogger
        {
            private readonly ProcessAssemblyTask _weaver;

            public WeaverLogger(ProcessAssemblyTask weaver)
            {
                _weaver = weaver ?? throw new ArgumentNullException(nameof(weaver));
            }

            public void Error(string message)
            {
                _weaver.LogError(message);
            }

            public void Debug(string message, DebugLogLevel debugLevel)
            {
                _weaver.InternalLogDebug(message, debugLevel);
            }

            public void Info(string message)
            {
                _weaver.LogInfo(message);
            }

            public void Warning(string message)
            {
                _weaver.LogWarning(message);
            }
        }
    }
}