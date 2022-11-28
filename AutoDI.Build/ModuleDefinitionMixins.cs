using System.Reflection;

using Mono.Cecil;
using Mono.Cecil.Cil;

using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoDI.Build;

internal static class ModuleDefinitionMixins
{
    public static TypeReference Get<T>(this ModuleDefinition module)
    {
        return module.ImportReference(typeof(T));
    }

    public static MethodDefinition CreateDefaultConstructor(this ModuleDefinition module, Type? baseType = null)
    {
        var ctor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));
        ILProcessor processor = ctor.Body.GetILProcessor();
        processor.Emit(OpCodes.Ldarg_0); //this

        ConstructorInfo baseCtor = (baseType ?? typeof(object))
            .GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(),
                null);
        if (baseCtor is null)
            throw new Exception($"Could not find constructor for '{(baseType ?? typeof(object)).FullName}'");
        processor.Emit(OpCodes.Call, module.ImportReference(baseCtor));
        processor.Emit(OpCodes.Ret);
        return ctor;
    }

    internal static MethodDefinition CreateStaticConstructor(this ModuleDefinition module)
    {
        var ctor = new MethodDefinition(".cctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.ImportReference(typeof(void)));

        return ctor;
    }

    public static FieldDefinition CreateStaticReadonlyField<T>(this ModuleDefinition moduleDefintion, string name, bool @public)
    {
        return moduleDefintion is null
            ? throw new ArgumentNullException(nameof(moduleDefintion))
            : name is null
            ? throw new ArgumentNullException(nameof(name))
            : moduleDefintion.CreateStaticReadonlyField(name, @public, moduleDefintion.Get<T>());
    }

    public static FieldDefinition CreateStaticReadonlyField(this ModuleDefinition moduleDefinition, string name,
        bool @public, TypeReference type)
    {
        return moduleDefinition is null
            ? throw new ArgumentNullException(nameof(moduleDefinition))
            : name is null
            ? throw new ArgumentNullException(nameof(name))
            : new FieldDefinition(name,
            (@public ? FieldAttributes.Public : FieldAttributes.Private) | FieldAttributes.Static |
            FieldAttributes.InitOnly, moduleDefinition.ImportReference(type));
    }

    public static FieldDefinition CreateReadonlyField<T>(this ModuleDefinition moduleDefinition, string name, bool @public)
    {
        return moduleDefinition is null
            ? throw new ArgumentNullException(nameof(moduleDefinition))
            : name is null
            ? throw new ArgumentNullException(nameof(name))
            : moduleDefinition.CreateReadonlyField(name, @public, moduleDefinition.Get<T>());
    }

    public static FieldDefinition CreateReadonlyField(this ModuleDefinition moduleDefinition,
        string name, bool @public, TypeReference type)
    {
        return moduleDefinition is null
            ? throw new ArgumentNullException(nameof(moduleDefinition))
            : name is null
            ? throw new ArgumentNullException(nameof(name))
            : new FieldDefinition(name,
            (@public ? FieldAttributes.Public : FieldAttributes.Private) |
            FieldAttributes.InitOnly, moduleDefinition.ImportReference(type));
    }

    public static MethodReference GetDefaultConstructor<T>(this ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.GetDefaultConstructor(typeof(T));
    }

    public static MethodReference GetDefaultConstructor(this ModuleDefinition moduleDefinition, Type targetType)
    {
        if (moduleDefinition is null) throw new ArgumentNullException(nameof(moduleDefinition));

        var defualtCtor = targetType.GetConstructor(Array.Empty<Type>());
        return defualtCtor is null
            ? throw new InvalidOperationException($"Could not find default constructor for '{targetType.FullName}'")
            : moduleDefinition.ImportReference(defualtCtor);
    }

    public static MethodReference GetConstructor<T>(this ModuleDefinition moduleDefinition, params Type[] argumentTypes)
    {
        return moduleDefinition.GetConstructor(typeof(T), argumentTypes);
    }

    public static MethodReference GetConstructor(this ModuleDefinition moduleDefinition, Type targetType, params Type[] argumentTypes)
    {
        if (moduleDefinition is null) throw new ArgumentNullException(nameof(moduleDefinition));

        var ctors = targetType.GetConstructors();

        if (argumentTypes?.Any() == true)
        {
            ctors = ctors.Where(c => c.GetParameters().Select(x => x.ParameterType).SequenceEqual(argumentTypes))
                .ToArray();
        }

        return ctors.Length switch
        {
            0 => throw new InvalidOperationException($"Could not find any matching constructors for '{targetType.FullName}'"),
            1 => moduleDefinition.ImportReference(ctors[0]),
            _ => throw new InvalidOperationException($"Found multiple matching constructors for '{targetType.FullName}'"),
        };
    }

    public static MethodReference GetMethod<TContainingType>(this ModuleDefinition moduleDefinition,
        string methodName)
    {
        return moduleDefinition.GetMethod(typeof(TContainingType), methodName);
    }

    public static MethodReference GetMethod(this ModuleDefinition moduleDefinition,
        Type containingType, string methodName)
    {
        if (moduleDefinition is null) throw new ArgumentNullException(nameof(moduleDefinition));
        if (containingType is null) throw new ArgumentNullException(nameof(containingType));
        if (methodName is null) throw new ArgumentNullException(nameof(methodName));

        MethodInfo method = containingType.GetMethod(methodName);
        return method is null
            ? throw new InvalidOperationException($"Could not find method '{methodName}' on '{containingType.FullName}'")
            : moduleDefinition.ImportReference(method);
    }
}