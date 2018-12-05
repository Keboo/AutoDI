using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace AutoDI.Build
{
    internal class AssemblyResolver : DefaultAssemblyResolver
    {
        public AssemblyResolver()
        {
            ResolveFailure += OnResolveFailure;
        }

        private AssemblyDefinition OnResolveFailure(object sender, AssemblyNameReference reference)
        {
            Assembly assembly;
            try
            {
#pragma warning disable 618
                assembly = Assembly.LoadWithPartialName(reference.Name);
#pragma warning restore 618
            }
            catch (FileNotFoundException)
            {
                assembly = null;
            }

            if (!string.IsNullOrWhiteSpace(assembly?.CodeBase))
            {
                string path = new Uri(assembly.CodeBase).AbsolutePath;
                var readerParameters = new ReaderParameters(ReadingMode.Deferred)
                {
                    ReadWrite = false,
                    ReadSymbols = false,
                    AssemblyResolver = this
                };
                return AssemblyDefinition.ReadAssembly(path, readerParameters);
            }
            return null;
        }
    }
}