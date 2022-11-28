namespace AutoDI.Build;

public partial class ProcessAssemblyTask
{
    private Settings? LoadSettings(AssemblyRewiteTaskContext context)
    {
        Settings settings;
        try
        {
            settings = Settings.Load(context.ModuleDefinition);
        }
        catch (SettingsParseException e)
        {
            context.Error($"Failed to parse AutoDI settings{Environment.NewLine}{e.Message}", null);
            return null;
        }

        context.DebugLogLevel = settings.DebugLogLevel;
        context.Debug($"Loaded settings\r\n{settings}", DebugLogLevel.Default);

        return settings;
    }
}