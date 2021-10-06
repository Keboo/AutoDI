using System;

namespace AutoDI.Build
{
    public partial class ProcessAssemblyTask
    {
        private Settings LoadSettings()
        {
            Settings settings;
            try
            {
                settings = Settings.Load(ModuleDefinition);
            }
            catch (SettingsParseException e)
            {
                var additionalInformation = StackTracer.GetStackTrace(e);
                Logger.Error($"Failed to parse AutoDI settings{Environment.NewLine}{e.Message}",additionalInformation);
                return null;
            }

            Logger.DebugLogLevel = settings.DebugLogLevel;
            Logger.Debug($"Loaded settings\r\n{settings}", DebugLogLevel.Default);

            return settings;
        }
    }
}