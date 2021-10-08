using System;
using System.Runtime.CompilerServices;
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

        public void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ErrorLogged = true;
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(
               subcategory: "",
               code: "",
               file: sourceFilePath,
               lineNumber: sourceLineNumber,
               columnNumber: 0,
               endLineNumber: 0,
               endColumnNumber: 0,
               message: $"{MessageSender} {message}",
               helpKeyword: "",
               senderName: MessageSender
               ));
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

        public void Warning(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            _task.BuildEngine.LogWarningEvent(new BuildWarningEventArgs(
                subcategory: "",
                code: "",
                file: sourceFilePath,
                lineNumber: sourceLineNumber,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: $"{MessageSender} {message}",
                helpKeyword: "",
                senderName: MessageSender
                ));
        }
    }
}