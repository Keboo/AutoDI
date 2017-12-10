using System;

namespace AutoDI.Fody
{
    public class SettingsParseException : AutoDIException
    {
        public SettingsParseException()
        { }

        public SettingsParseException(string message) 
            : base(message)
        { }

        public SettingsParseException(string message, Exception innerException) 
            : base(message, innerException)
        { }
    }
}