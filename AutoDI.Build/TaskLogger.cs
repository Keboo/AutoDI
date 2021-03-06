﻿using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AutoDI.Build
{
    internal class TaskLogger : ILogger
    {
        private readonly Task _task;

        public bool ErrorLogged { get; private set; }

        public DebugLogLevel DebugLogLevel { get; set; }

        private const string MessageSender = "AutoDI:";

        public TaskLogger(Task task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public void Error(string message)
        {
            ErrorLogged = true;
            _task.BuildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", null, 0, 0, 0, 0, $"{MessageSender} {message}", "", MessageSender));
        }

        public void Debug(string message, DebugLogLevel debugLevel)
        {
            if (debugLevel >= DebugLogLevel)
            {
                _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"{MessageSender} {message}", "", MessageSender, MessageImportance.Low));
            }
        }

        public void Info(string message)
        {
            _task.BuildEngine.LogMessageEvent(new BuildMessageEventArgs($"{MessageSender} {message}", "", MessageSender, MessageImportance.Normal));
        }

        public void Warning(string message)
        {
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs("", "", null, 0, 0, 0, 0, $"{MessageSender} {message}", "", MessageSender));
        }
    }
}