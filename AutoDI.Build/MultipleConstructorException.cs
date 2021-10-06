using Mono.Cecil;

namespace AutoDI.Build
{
    internal class MultipleConstructorException : AutoDIBuildException
    {
        public MethodDefinition DuplicateConstructor { get; }

        public MultipleConstructorException(string message, MethodDefinition duplicateContructor) : base(message)
        {
            DuplicateConstructor = duplicateContructor;
        }
    }
}
