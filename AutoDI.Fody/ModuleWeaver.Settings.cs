
using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using System.Linq;

// ReSharper disable once CheckNamespace
public partial class ModuleWeaver
{
    private Settings LoadSettings()
    {
        var settings = new Settings();
        foreach (CustomAttribute attribute in GetAllModules().SelectMany(m => m.Assembly.CustomAttributes))
        {
            if (attribute.AttributeType.IsType<SettingsAttribute>())
            {
                foreach (CustomAttributeNamedArgument property in attribute.Properties)
                {
                    if (property.Argument.Value != null)
                    {
                        switch (property.Name)
                        {
                            case nameof(SettingsAttribute.AutoInit):
                                settings.AutoInit = (bool)property.Argument.Value;
                                break;
                            case nameof(SettingsAttribute.Behavior):
                                settings.Behavior = (Behaviors)property.Argument.Value;
                                break;
                            case nameof(SettingsAttribute.DebugLogLevel):
                                settings.DebugLogLevel = (DebugLogLevel)property.Argument.Value;
                                break;
                            case nameof(SettingsAttribute.GenerateRegistrations):
                                settings.GenerateRegistrations = (bool)property.Argument.Value;
                                break;
                        }
                    }
                }
            }
        }

        settings = Settings.Parse(settings, Config);
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