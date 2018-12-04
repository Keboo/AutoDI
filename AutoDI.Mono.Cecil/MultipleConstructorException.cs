namespace AutoDI.Mono.Cecil
{
    internal class MultipleConstructorException : AutoDIBuildException
    {
        public MultipleConstructorException(string message) : base(message)
        {
        }
    }
}
