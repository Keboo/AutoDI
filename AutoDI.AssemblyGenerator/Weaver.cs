using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoDI.AssemblyGenerator
{
    public sealed class Weaver
    {
        public static Weaver FindWeaver(string weaverName)
        {
            object ProcessAssembly(Assembly assembly)
            {
                //TODO: Check for BaseModuleWeaver type
                Type weaverType = assembly.GetType(weaverName);
                
                if (weaverType == null) return null;
                return Activator.CreateInstance(weaverType);
            }

            string assemblyName = $"AutoDI.Build";

            try
            {
                var weaverInstance = (Task)ProcessAssembly(Assembly.Load(assemblyName));
                if (weaverInstance != null)
                    return new Weaver(weaverName, weaverInstance);
            }
            catch (Exception)
            {
                // ignored
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName == assemblyName))
            {
               // var weaverInstance = (BaseModuleWeaver)ProcessAssembly(assembly);
               // if (weaverInstance != null)
               //     return new Weaver(weaverName, weaverInstance);
            }

            throw new Exception($"Failed to find weaver '{weaverName}'. Could not locate {weaverName}.Fody assembly.");
        }

        public Task Instance { get; }
        public string Name { get; }

        private TypeCache TypeResolver { get; } = new TypeCache();


        internal Weaver(string name, Task taskInstance)
        {
            Instance = taskInstance ?? throw new ArgumentNullException(nameof(taskInstance));
            Name = name;
        }

        //public void ApplyToAssembly(string assemblyPath)
        //{
        //    using (var fileStream = File.Open(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
        //    {
        //        ApplyToAssembly(fileStream);
        //    }
        //}

        public void ApplyToAssembly(string assemblyFilePath)
        {
            dynamic task = Instance;
            task.AssemblyFile = assemblyFilePath;
            task.BuildEngine = new InMemoryBuildEngine();
            bool result = task.Execute();

            if (!result)
            {
                throw new Exception("Task did not succeed");
            }

            //foreach (var assemblyName in Instance.GetAssembliesForScanning())
            //{
            //    var assembly = assemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
            //    if (assembly == null)
            //    {
            //        continue;
            //    }
            //
            //    if (assemblyDefinitions.ContainsKey(assemblyName))
            //    {
            //        continue;
            //    }
            //    assemblyDefinitions.Add(assemblyName, assembly);
            //}
            //TypeResolver.Initialise(assemblyDefinitions.Values);
            //
            //
            //var module = ModuleDefinition.ReadModule(assemblyStream);
            //Instance.ModuleDefinition = module;
            //
            //Instance.FindType = TypeResolver.FindType;
            //Instance.TryFindType = TypeResolver.TryFindType;
            //Instance.ResolveAssembly = assemblyName =>
            //    assemblyResolver.Resolve(new AssemblyNameReference(assemblyName, null));
            //var errors = new List<string>();
            //Instance.LogError = s =>
            //{
            //    Debug.WriteLine($" Error: {s}");
            //    errors.Add(s);
            //};
            //Instance.LogWarning = s => Debug.WriteLine($" Warning: {s}");
            //Instance.LogInfo = s => Debug.WriteLine($" Info: {s}");
            //Instance.LogDebug = s => Debug.WriteLine($" Debug: {s}");
            //Instance.Execute();
            //if (errors.Any())
            //{
            //    throw new WeaverErrorException(errors);
            //}
            //Instance.AfterWeaving();

            //assemblyStream.Position = 0;
            //module.Write(assemblyStream);
        }

        //Copy of https://github.com/Fody/Fody/blob/master/FodyHelpers/TypeCache.cs
        private class TypeCache
        {
            private readonly Dictionary<string, TypeDefinition> _CachedTypes = new Dictionary<string, TypeDefinition>();

            public void Initialise(IEnumerable<AssemblyDefinition> assemblyDefinitions)
            {
                var definitions = assemblyDefinitions.ToList();
                foreach (AssemblyDefinition assembly in definitions)
                {
                    foreach (TypeDefinition type in assembly.MainModule.GetTypes())
                    {
                        AddIfPublic(type);
                    }
                }

                foreach (AssemblyDefinition assembly in definitions)
                {
                    foreach (ExportedType exportedType in assembly.MainModule.ExportedTypes)
                    {
                        if (definitions.Any(x => x.Name.Name == exportedType.Scope.Name))
                        {
                            continue;
                        }

                        var typeDefinition = exportedType.Resolve();
                        if (typeDefinition == null)
                        {
                            continue;
                        }

                        AddIfPublic(typeDefinition);
                    }
                }
            }

            public virtual TypeDefinition FindType(string typeName)
            {
                if (_CachedTypes.TryGetValue(typeName, out var type))
                {
                    return type;
                }

                if (FindFromValues(typeName, out type))
                {
                    return type;
                }

                throw new Exception($"Could not find '{typeName}'.");
            }

            private bool FindFromValues(string typeName, out TypeDefinition type)
            {
                if (!typeName.Contains('.'))
                {
                    var types = _CachedTypes.Values
                        .Where(x => x.Name == typeName)
                        .ToList();
                    if (types.Count > 1)
                    {
                        throw new Exception($"Found multiple types for '{typeName}'.");
                    }
                    if (types.Count == 0)
                    {
                        type = null;
                        return false;
                    }

                    type = types[0];
                    return true;
                }

                type = null;
                return false;
            }

            public virtual bool TryFindType(string typeName, out TypeDefinition type)
            {
                if (_CachedTypes.TryGetValue(typeName, out type))
                {
                    return true;
                }

                return FindFromValues(typeName, out type);
            }

            private void AddIfPublic(TypeDefinition type)
            {
                if (!type.IsPublic)
                {
                    return;
                }
                if (_CachedTypes.ContainsKey(type.FullName))
                {
                    return;
                }

                _CachedTypes.Add(type.FullName, type);
            }
        }

        private class InMemoryBuildEngine : IBuildEngine
        {
            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
                
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
                
            }

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
                throw new NotImplementedException();
            }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
                IDictionary targetOutputs)
            {
                throw new NotImplementedException();
            }

            public bool ContinueOnError { get; } = false;
            public int LineNumberOfTaskNode { get; } = 0;
            public int ColumnNumberOfTaskNode { get; } = 0;
            public string ProjectFileOfTaskNode { get; } = "";
        }
    }
}
