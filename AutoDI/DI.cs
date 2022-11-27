using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace AutoDI;

public static class DI
{
    public static void Init(Assembly? containerAssembly = null, Action<IApplicationBuilder>? configureMethod = null)
    {
        Type autoDI = GetAutoDIType(containerAssembly);

        var method = autoDI.GetRuntimeMethod(nameof(Init), new[] { typeof(Action<IApplicationBuilder>) });
        if (method is null) throw new RequiredMethodMissingException($"Could not find {nameof(Init)} method on {autoDI.FullName}");
        method.Invoke(null, new object?[] { configureMethod });
    }

    public static bool TryInit(Assembly? containerAssembly = null,
        Action<IApplicationBuilder>? configureMethod = null)
    {
        try
        {
            Init(containerAssembly, configureMethod);
            return true;
        }
        catch (TargetInvocationException e)
            when (e.InnerException is AlreadyInitializedException)
        {
            return false;
        }
    }

    public static void AddServices(IServiceCollection collection, Assembly? containerAssembly = null)
    {
        Type autoDI = GetAutoDIType(containerAssembly);

        var method = autoDI.GetRuntimeMethod(nameof(AddServices), new[] { typeof(IServiceCollection) });
        if (method is null) throw new RequiredMethodMissingException($"Could not find {nameof(AddServices)} method on {autoDI.FullName}");
        method.Invoke(null, new object[] { collection });
    }

    public static void Dispose(Assembly? containerAssembly = null)
    {
        Type autoDI = GetAutoDIType(containerAssembly);

        var method = autoDI.GetRuntimeMethod(nameof(Dispose), Array.Empty<Type>());
        if (method is null) throw new RequiredMethodMissingException($"Could not find {nameof(Dispose)} method on {autoDI.FullName}");
        method.Invoke(null, Array.Empty<object>());
    }

    public static IServiceProvider GetGlobalServiceProvider(Assembly assembly)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));

        Type autoDI = GetAutoDIType(assembly);
        FieldInfo field = autoDI.GetRuntimeFields().SingleOrDefault(f => f.Name == Constants.GlobalServiceProviderName) ??
                    throw new GlobalServiceProviderNotFoundException($"Could not find {Constants.GlobalServiceProviderName} field");
        return (IServiceProvider)field.GetValue(null);
    }

    private static Type GetAutoDIType(Assembly? containerAssembly)
    {
        const string typeName = Constants.Namespace + "." + Constants.TypeName;

        Type containerType = containerAssembly != null
            ? containerAssembly.GetType(typeName)
            : Type.GetType(typeName);

        return containerType is null
            ? throw new GeneratedClassMissingException("Could not find generated AutoDI class. Was the AutoDI.Build run on this assembly?")
            : containerType;
    }

}