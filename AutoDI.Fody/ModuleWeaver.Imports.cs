
using AutoDI;
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

    private class Imports
    {
        public Imports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
            TypeDefinition appBuilderType = findType(typeof(ApplicationBuilder).FullName);
            MethodDefinition buildMethod =
                appBuilderType.GetMethods().Single(m => m.Name == nameof(ApplicationBuilder.Build));

            var iapplicationBuilder = findType(typeof(IApplicationBuilder).FullName);
            IApplicationBuilder_ConfigureServices = moduleDefinition.ImportReference(
                iapplicationBuilder
                .GetMethods()
                .Single(x => x.Name == nameof(IApplicationBuilder.ConfigureServices)));

            IServiceProvider = moduleDefinition.ImportReference(buildMethod.ReturnType);

            IServiceCollection = moduleDefinition.ImportReference(findType("Microsoft.Extensions.DependencyInjection.IServiceCollection"));

            var coreType = findType("System.Type");
            System_Type = moduleDefinition.ImportReference(coreType);
            Type_GetTypeFromHandle =
                moduleDefinition.ImportReference(coreType.GetMethods().Single(m => m.Name == "GetTypeFromHandle"));
            System_Func2_Ctor =
                moduleDefinition.ImportReference(findType("System.Func`2")).Resolve().GetConstructors().Single();
            System_Action_Ctor = moduleDefinition.ImportReference(findType("System.Action`1")).Resolve()
                .GetConstructors().Single();

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
                UpdateMethod(findType(typeof(ServiceCollectionMixins).FullName)
                    .GetMethods().Single(m => m.Name == nameof(ServiceCollectionMixins.AddAutoDIService))));
            
            var serviceProviderExtensions = moduleDefinition.ImportReference(findType("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions")).Resolve();
            ServiceProviderServiceExtensions_GetService = moduleDefinition.ImportReference(serviceProviderExtensions.Methods.Single(x => x.Name == "GetService"));

            var globalDiType = findType(typeof(GlobalDI).FullName);
            if (globalDiType == null)
                throw new AutoDIException($"Could not find '{typeof(GlobalDI).FullName}'");

            GlobalDI_Register = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == nameof(GlobalDI.Register))));
            GlobalDI_Unregister = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == nameof(GlobalDI.Unregister))));
            GlobalDI_GetService = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m =>
                    m.Name == nameof(GlobalDI.GetService) && m.HasGenericParameters && m.Parameters.Count == 1)));

            var autoDIExceptionType = moduleDefinition
                .ImportReference(findType(typeof(AutoDIException).FullName)).Resolve();
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

        public MethodReference IApplicationBuilder_ConfigureServices { get; }

        public MethodReference Type_GetTypeFromHandle { get; }

        public MethodReference ServiceCollectionMixins_AddAutoDIService { get; }

        public MethodReference ServiceProviderServiceExtensions_GetService { get; }

        public TypeReference IServiceCollection { get; }

        public TypeReference IServiceProvider { get; }

        public TypeReference System_Type { get; }

        public TypeReference System_Exception { get; }
        public MethodReference System_AggregateException_Ctor { get; }
        public MethodReference System_Func2_Ctor { get; }
        public MethodReference System_Action_Ctor { get; }

        public TypeDefinition List_Type { get; }

        public MethodReference AutoDIException_Ctor { get; }
    }
}