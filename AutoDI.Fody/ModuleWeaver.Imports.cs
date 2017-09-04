
using System;
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
            
            ServiceProviderMixins_GetService = moduleDefinition.ImportReference(
                UpdateMethod(
                    autoDIAssembly.MainModule.GetType(typeof(ServiceProviderMixins).FullName).GetMethods()
                .Single(m => m.Name == nameof(ServiceProviderMixins.GetService) && m.HasGenericParameters)));

            Type_GetTypeFromHandle = moduleDefinition.ImportReference(coreType.GetMethods().Single(m => m.Name == nameof(Type.GetTypeFromHandle)));

            ServiceCollectionMixins_AddAutoDIService = moduleDefinition.ImportReference(
                UpdateMethod(autoDIAssembly.MainModule.GetType(typeof(ServiceCollectionMixins).FullName)
                    .GetMethods().Single(m => m.Name == nameof(ServiceCollectionMixins.AddAutoDIService))));

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


        public MethodReference ServiceProviderMixins_GetService { get; }

        public MethodReference Type_GetTypeFromHandle { get; }

        public MethodReference ServiceCollectionMixins_AddAutoDIService { get; }

        public TypeReference IServiceProvider { get; }

        public TypeReference System_Type { get; }
    }
}