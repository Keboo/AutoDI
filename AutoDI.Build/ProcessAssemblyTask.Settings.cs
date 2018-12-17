using System;

namespace AutoDI.Build
{
    public partial class ProcessAssemblyTask
    {
        private Settings LoadSettings(TypeResolver typeResolver)
        {
            Settings settings;
            try
            {
                settings = Settings.Load(typeResolver);
            }
            catch (SettingsParseException e)
            {
                Logger.Error($"Failed to parse AutoDI settings{Environment.NewLine}{e.Message}");
                return null;
            }

            Logger.DebugLogLevel = settings.DebugLogLevel;
            Logger.Debug($"Loaded settings\r\n{settings}", DebugLogLevel.Default);

            return settings;
        }
    }
}