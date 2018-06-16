namespace AutoDI.Fody
{
    internal class MultipleConstructorException : AutoDIBuildException
    {
        public MultipleConstructorException(string message) : base(message)
        {
        }
    }
}
