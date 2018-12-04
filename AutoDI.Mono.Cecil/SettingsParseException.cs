namespace AutoDI.Mono.Cecil
{
    internal class SettingsParseException : AutoDIBuildException
    {
        public SettingsParseException()
        { }

        public SettingsParseException(string message) 
            : base(message)
        { }
    }
}