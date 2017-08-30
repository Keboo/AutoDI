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

    public class AutoDINotInitializedException : AutoDIException
    {
        public AutoDINotInitializedException()
            : base("AutoDI has not been initialized")
        { }
    }

    public class AutoDIAlreadyInitializedException : AutoDIException
    {
        public AutoDIAlreadyInitializedException()
            : base("AutoDI has already been initialized. Call Dispose before trying to initialize a second time.")
        { }
    }
}