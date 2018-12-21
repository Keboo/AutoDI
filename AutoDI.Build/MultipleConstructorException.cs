namespace AutoDI.Build
{
    internal class MultipleConstructorException : AutoDIBuildException
    {
        public MultipleConstructorException(string message) : base(message)
        {
        }
    }
}
