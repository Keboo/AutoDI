using System;

namespace AutoDI
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SettingsAttribute : Attribute
    {
        public Behaviors Behavior { get; set; }
        public bool AutoInit { get; set; }
        public bool AutoLibraryInit { get; set; }
        public bool GenerateRegistrations { get; set; }
        public bool DebugExceptions { get; set; }
        public DebugLogLevel DebugLogLevel { get; set; }
        public CodeLanguage DebugCodeGeneration { get; set; }

        //Types, Maps, Assemblies
    }
}