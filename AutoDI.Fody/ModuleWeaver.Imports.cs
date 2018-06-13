
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

// ReSharper disable once CheckNamespace
public partial class ModuleWeaver
{
    private Imports Import { get; set; }

    private void LoadRequiredData()
    {
        if (Import == null)
        {
            Import = new Imports(FindType, ModuleDefinition);
        }
    }

    internal class Imports
    {
        public Imports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
            System = new SystemImport(findType, moduleDefinition);
            IApplicationBuilder = new IApplicationBuilderImport(findType, moduleDefinition);
            ApplicationBuilder = new ApplicationBuilderImport(findType, moduleDefinition);
            AutoDI = new AutoDIImport(findType, moduleDefinition);

            IServiceProvider = moduleDefinition.ImportReference(findType("System.IServiceProvider"));

            IServiceCollection = moduleDefinition.ImportReference(findType("Microsoft.Extensions.DependencyInjection.IServiceCollection"));

            var coreType = findType("System.Type");
            System_Type = moduleDefinition.ImportReference(coreType);
            Type_GetTypeFromHandle =
                moduleDefinition.ImportReference(coreType.GetMethods().Single(m => m.Name == "GetTypeFromHandle"));
            System_Func2_Ctor =
                moduleDefinition.ImportReference(findType("System.Func`2")).Resolve().GetConstructors().Single();

            System_Exception = moduleDefinition.ImportReference(findType("System.Exception"));

            List_Type = findType("System.Collections.Generic.List`1");

            var aggregateExceptionType = findType("System.AggregateException").Resolve();
            var enumerableType = findType("System.Collections.Generic.IEnumerable`1");
            var enumerableException = enumerableType.MakeGenericInstanceType(System_Exception);

            System_AggregateException_Ctor = moduleDefinition.ImportReference(aggregateExceptionType
                .GetConstructors().Single(c =>
                    c.Parameters.Count == 2 &&
                    c.Parameters[0].ParameterType.IsType<string>() &&
                    c.Parameters[1].ParameterType.IsType(enumerableException)));

            ServiceCollectionMixins_AddAutoDIService = moduleDefinition.ImportReference(
                UpdateMethod(findType("AutoDI.ServiceCollectionMixins")
                    .GetMethods().Single(m => m.Name == "AddAutoDIService")));
            
            var serviceProviderExtensions = moduleDefinition.ImportReference(findType("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions")).Resolve();
            ServiceProviderServiceExtensions_GetService = moduleDefinition.ImportReference(serviceProviderExtensions.Methods.Single(x => x.Name == "GetService"));

            var globalDiType = findType("AutoDI.GlobalDI");
            if (globalDiType == null)
                throw new AutoDIBuildException("Could not find 'AutoDI.GlobalDI'");

            GlobalDI_Register = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == "Register")));
            GlobalDI_Unregister = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == "Unregister")));
            GlobalDI_GetService = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m =>
                    m.Name == "GetService" && m.HasGenericParameters && m.Parameters.Count == 1)));

            var autoDIExceptionType = moduleDefinition
                .ImportReference(findType("AutoDI.AutoDIException")).Resolve();
            AutoDIException_Ctor = moduleDefinition.ImportReference(autoDIExceptionType.GetConstructors().Single(c =>
                c.Parameters.Count == 2 && c.Parameters[0].ParameterType.IsType<string>() &&
                c.Parameters[1].ParameterType.IsType<Exception>()));

            MethodReference UpdateMethod(MethodReference method)
            {
                method.ReturnType = UpdateType(method.ReturnType);
                foreach (ParameterDefinition parameter in method.Parameters)
                {
                    parameter.ParameterType = UpdateType(parameter.ParameterType);
                }

                return method;
            }

            TypeReference UpdateType(TypeReference type)
            {
                if (TypeComparer.FullName.Equals(type, IServiceProvider))
                {
                    return IServiceProvider;
                }

                if (TypeComparer.FullName.Equals(type, System_Type))
                {
                    return System_Type;
                }

                if (type.IsArray)
                {
                    var arrayType = UpdateType(type.GetElementType());
                    return new ArrayType(arrayType);
                }

                return type;
            }
        }

        public MethodReference GlobalDI_Register { get; }
        public MethodReference GlobalDI_Unregister { get; }
        public MethodReference GlobalDI_GetService { get; }

        public IApplicationBuilderImport IApplicationBuilder { get; }

        public ApplicationBuilderImport ApplicationBuilder { get; }

        public SystemImport System { get; }

        public AutoDIImport AutoDI { get; }

        public class IApplicationBuilderImport
        {
            public const string TypeName = "AutoDI.IApplicationBuilder";

            public IApplicationBuilderImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Type = moduleDefinition.ImportReference(findType(TypeName));

                TypeDefinition resolved = Type.Resolve();
                ConfigureServices = moduleDefinition.ImportReference(resolved
                        .GetMethods()
                        .Single(x => x.Name == "ConfigureServices"));
                Build = moduleDefinition.ImportReference(resolved.GetMethods().Single(x => x.Name == "Build"));
            }

            public TypeReference Type { get; }

            public MethodReference ConfigureServices { get; }

            public MethodReference Build { get; }
        }

        public class ApplicationBuilderImport
        {
            public ApplicationBuilderImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Type = findType("AutoDI.ApplicationBuilder");

                Ctor = moduleDefinition.ImportReference(Type.GetConstructors().Single(x => !x.HasParameters));
            }

            public TypeDefinition Type { get; }

            public MethodReference Ctor { get; }
        }

        public class SystemImport
        {
            public SystemImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Action = new ActionImport(findType, moduleDefinition);
            }

            public ActionImport Action { get; }

            public class ActionImport
            {
                public TypeReference Type { get; }

                public MethodReference Ctor { get; }

                public MethodReference Invoke { get; }

                public ActionImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    Type = moduleDefinition.ImportReference(findType("System.Action`1"));

                    var resolved = Type.Resolve();

                    Invoke = moduleDefinition.ImportReference(resolved.GetMethods().Single(x => x.Name == "Invoke"));
                    
                    Ctor = moduleDefinition.ImportReference(resolved.GetConstructors().Single());
                }
            }
        }

        public class AutoDIImport
        {
            public AutoDIExceptionsImport Exceptions { get; }

            public TypeReference DependencyAttributeType { get; }

            public AutoDIImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Exceptions = new AutoDIExceptionsImport(findType, moduleDefinition);

                DependencyAttributeType = moduleDefinition.ImportReference(findType("AutoDI.DependencyAttribute"));
            }

            public class AutoDIExceptionsImport
            {
                public AutoDIExceptionsImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    TypeDefinition alreadyInitialized = findType("AutoDI.AlreadyInitializedException");
                    AlreadyInitializedException_Ctor = moduleDefinition.ImportReference(alreadyInitialized.GetConstructors().Single(x => !x.HasParameters));
                }

                public MethodReference AlreadyInitializedException_Ctor { get; }
            }
        }

        public MethodReference Type_GetTypeFromHandle { get; }

        public MethodReference ServiceCollectionMixins_AddAutoDIService { get; }

        public MethodReference ServiceProviderServiceExtensions_GetService { get; }

        public TypeReference IServiceCollection { get; }

        public TypeReference IServiceProvider { get; }

        public TypeReference System_Type { get; }

        public TypeReference System_Exception { get; }
        public MethodReference System_AggregateException_Ctor { get; }
        public MethodReference System_Func2_Ctor { get; }

        public TypeDefinition List_Type { get; }

        public MethodReference AutoDIException_Ctor { get; }
    }
}