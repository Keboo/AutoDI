using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Cecil.Cil;

namespace AutoDI.Build;

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

    public void Error(string message, SequencePoint? sequencePoint)
    {
        BuildErrorEventArgs buildErrorEventArgs;
        ErrorLogged = true;

        buildErrorEventArgs = sequencePoint is null
            ? new BuildErrorEventArgs("", "", null, 0, 0, 0, 0, $"{MessageSender} {message}", "", MessageSender)
            : new BuildErrorEventArgs("", "", sequencePoint.Document.Url, sequencePoint.StartLine, sequencePoint.StartColumn, sequencePoint.EndLine, sequencePoint.EndColumn, $"{MessageSender} {message}", "", MessageSender);

        _task.BuildEngine.LogErrorEvent(buildErrorEventArgs);
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

    public void Warning(string message, SequencePoint? sequencePoint)
    {
        BuildWarningEventArgs buildWarningEventArgs = sequencePoint == null
            ? new BuildWarningEventArgs("", "", null, 0, 0, 0, 0, $"{MessageSender} {message}", "", MessageSender)
            : new BuildWarningEventArgs("", "", sequencePoint.Document.Url, sequencePoint.StartLine, sequencePoint.StartColumn, sequencePoint.EndLine, sequencePoint.EndColumn, $"{MessageSender} {message}", "", MessageSender);
        _task.BuildEngine.LogWarningEvent(buildWarningEventArgs);
    }
}