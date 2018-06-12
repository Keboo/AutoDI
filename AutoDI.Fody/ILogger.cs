﻿namespace AutoDI.Fody
{
    public interface ILogger
    {
        void Debug(string message, AutoDI.DebugLogLevel debugLevel);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}