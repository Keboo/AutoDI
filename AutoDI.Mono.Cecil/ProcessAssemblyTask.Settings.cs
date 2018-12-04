using System;

namespace AutoDI.Mono.Cecil
{
    public partial class ProcessAssemblyTask
    {
        private Settings LoadSettings(TypeResolver typeResolver)
        {
            Settings settings;
            try
            {
                settings = Settings.Load(typeResolver, Config);
            }
            catch (SettingsParseException e)
            {
                Logger.Error($"Failed to parse AutoDI settings{Environment.NewLine}{e.Message}");
                return null;
            }
            InternalLogDebug = (s, l) =>
            {
                if (l <= settings.DebugLogLevel)
                {
                    LogDebug(s);
                }
            };
            Logger.Debug($"Loaded settings\r\n{settings}", DebugLogLevel.Default);

            return settings;
        }
    }
}