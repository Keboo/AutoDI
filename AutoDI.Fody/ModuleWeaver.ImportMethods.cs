// ReSharper disable once CheckNamespace

using System;
using System.Linq;
using AutoDI;
using Mono.Cecil;

public partial class ModuleWeaver
{
    private ImportMethods Methods { get; set; }

    private void LoadMethods()
    {
        if (Methods == null)
        {
            Methods = new ImportMethods(ModuleDefinition);
        }
    }

    private class ImportMethods
    {
        public ImportMethods(ModuleDefinition moduleDefinition)
        {
            ServiceProviderMixins_GetService = moduleDefinition.ImportReference(
                (from method in typeof(ServiceProviderMixins).GetMethods()
                    where method.Name == nameof(ServiceProviderMixins.GetService) &&
                          method.IsGenericMethodDefinition
                    let parameters = method.GetParameters()
                    where parameters.Length == 2 && parameters[0].ParameterType == typeof(IServiceProvider) &&
                          parameters[1].ParameterType == typeof(object[])
                    select method).Single());
        }

        public MethodReference ServiceProviderMixins_GetService { get; }

    }
}