using System;

namespace AutoDI.Fody
{
    public class MultipleConstructorException : AutoDIException
    {
        public MultipleConstructorException()
        {
        }

        public MultipleConstructorException(string message) : base(message)
        {
        }

        public MultipleConstructorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
