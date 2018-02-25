using System;
using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using System.Linq;

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
            LogError($"Failed to parse AutoDI settings from FodyWeavers.xml{Environment.NewLine}{e.Message}");
            return null;
        }
        InternalLogDebug = (s, l) =>
        {
            if (l <= settings.DebugLogLevel)
            {
                LogDebug(s);
            }
        };
        InternalLogDebug($"Loaded settings\r\n{settings}", DebugLogLevel.Default);

        return settings;
    }
}