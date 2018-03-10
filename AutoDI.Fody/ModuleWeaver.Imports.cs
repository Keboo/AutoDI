
using System;
using System.Collections.Generic;
using System.Linq;
using AutoDI;
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

// ReSharper disable once CheckNamespace
public partial class ModuleWeaver
{
    private Imports Import { get; set; }

    private void LoadRequiredData(AssemblyDefinition autoDIAssembly)
    {
        if (Import == null)
        {
            Import = new Imports(ModuleDefinition, autoDIAssembly);
        }
    }

    private class Imports
    {
        public Imports(ModuleDefinition moduleDefinition, AssemblyDefinition autoDIAssembly)
        {
            TypeDefinition appBuilderType = autoDIAssembly.MainModule.GetType(typeof(ApplicationBuilder).FullName);
            MethodDefinition buildMethod = appBuilderType.GetMethods().Single(m => m.Name == nameof(ApplicationBuilder.Build));
            IServiceProvider = moduleDefinition.ImportReference(buildMethod.ReturnType);

            var coreType = moduleDefinition.ResolveCoreType(typeof(Type));
            System_Type = moduleDefinition.ImportReference(coreType);
            Type_GetTypeFromHandle = moduleDefinition.ImportReference(coreType.GetMethods().Single(m => m.Name == nameof(Type.GetTypeFromHandle)));
            
            System_Exception = moduleDefinition.ImportReference(moduleDefinition.ResolveCoreType(typeof(Exception)));

            List_Type = moduleDefinition.ResolveCoreType(typeof(List<>));

            var aggregateExceptionType = moduleDefinition.ResolveCoreType(typeof(AggregateException)).Resolve();
            var enumerableExceptionType = moduleDefinition.ImportReference(typeof(IEnumerable<Exception>));
            System_AggregateException_Ctor = moduleDefinition.ImportReference(aggregateExceptionType
                .GetConstructors().Single(c =>
                    c.Parameters.Count == 2 && c.Parameters[0].ParameterType.IsType<string>() &&
                    c.Parameters[1].ParameterType.IsType(enumerableExceptionType)));

            ServiceCollectionMixins_AddAutoDIService = moduleDefinition.ImportReference(
                UpdateMethod(autoDIAssembly.MainModule.GetType(typeof(ServiceCollectionMixins).FullName)
                    .GetMethods().Single(m => m.Name == nameof(ServiceCollectionMixins.AddAutoDIService))));

            var globalDiType = autoDIAssembly.MainModule.GetType(typeof(GlobalDI).FullName);
            if (globalDiType == null)
                throw new AutoDIException($"Could not find '{typeof(GlobalDI).FullName}' in '{autoDIAssembly.FullName}' - {autoDIAssembly.MainModule.FileName}");
            GlobalDI_Register = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == nameof(GlobalDI.Register))));
            GlobalDI_Unregister = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == nameof(GlobalDI.Unregister))));
            GlobalDI_GetService = moduleDefinition.ImportReference(UpdateMethod(globalDiType.GetMethods()
                .Single(m => m.Name == nameof(GlobalDI.GetService) && m.HasGenericParameters && m.Parameters.Count == 1)));

            var autoDIExceptionType = moduleDefinition
                .ImportReference(autoDIAssembly.MainModule.GetType(typeof(AutoDIException).FullName)).Resolve();
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
        
        public MethodReference Type_GetTypeFromHandle { get; }

        public MethodReference ServiceCollectionMixins_AddAutoDIService { get; }

        public TypeReference IServiceProvider { get; }

        public TypeReference System_Type { get; }

        public TypeReference System_Exception { get; }
        public MethodReference System_AggregateException_Ctor { get; }

        public TypeDefinition List_Type { get; }

        public MethodReference AutoDIException_Ctor { get; }
    }
}