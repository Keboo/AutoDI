using Mono.Cecil.Cil;

namespace AutoDI.Build;

internal class MultipleConstructorException : AutoDIBuildException
{
    public SequencePoint SequencePoint { get; }
    public MultipleConstructorException(string message, SequencePoint sequencePoint) : base(message)
    {
        SequencePoint = sequencePoint;
    }
}