using System;

namespace AutoDI.Fody
{
    public class MultipleConstructorAutoDIException : AutoDIException
    {
        public MultipleConstructorAutoDIException()
        {
        }

        public MultipleConstructorAutoDIException(string message) : base(message)
        {
        }

        public MultipleConstructorAutoDIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
