using System;

namespace AutoDI
{
    public class AutoDIException : Exception
    {
        public AutoDIException()
        { }

        public AutoDIException(string message) : base(message)
        { }

        public AutoDIException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class AutoDIInitializationException : AutoDIException
    {
        public AutoDIInitializationException()
            : base("AutoDI has not been initialized")
        { }
    }
}