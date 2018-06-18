using AutoDI;
using AutoDI.Fody;
using System;

// ReSharper disable once CheckNamespace
public partial class ModuleWeaver
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
            Logger.Error($"Failed to parse AutoDI settings from FodyWeavers.xml{Environment.NewLine}{e.Message}");
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