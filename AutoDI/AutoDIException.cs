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

    public class NotInitializedException : AutoDIException
    {
        public NotInitializedException()
            : base("AutoDI has not been initialized")
        { }
    }

    public class AlreadyInitializedException : AutoDIException
    {
        public AlreadyInitializedException()
            : base("AutoDI has already been initialized. Call Dispose before trying to initialize a second time.")
        { }
    }

    public class NoRegisteredContainerException : AutoDIException
    {
        public NoRegisteredContainerException(string message) :base(message)
        { }
    }

    public class NoServiceProviderFactoryException : AutoDIException
    {
        public NoServiceProviderFactoryException(string message)
            : base(message)
        { }
    }

    public class RequiredMethodMissingException : AutoDIException
    {
        public RequiredMethodMissingException(string message)
            : base(message)
        { }
    }

    public class GeneratedClassMissingException : AutoDIException
    {
        public GeneratedClassMissingException(string message)
            : base(message)
        { }
    }

    public class GlobalServiceProviderNotFoundException : AutoDIException
    {
        public GlobalServiceProviderNotFoundException(string message)
            : base(message)
        { }
    }
}