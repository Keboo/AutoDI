
using AutoDI.Fody;
using System;
using AutoDI;

partial class ModuleWeaver
{
    private ILogger Logger { get; set; }

    private class WeaverLogger : ILogger
    {
        private readonly ModuleWeaver _weaver;

        public WeaverLogger(ModuleWeaver weaver)
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