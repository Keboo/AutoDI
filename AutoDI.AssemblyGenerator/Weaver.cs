using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace AutoDI.AssemblyGenerator
{
    public sealed class Weaver
    {
        public static Weaver FindWeaver(string weaverTypeName)
        {
            object ProcessAssembly(Assembly assembly)
            {
                //TODO: Check for BaseModuleWeaver type
                Type weaverType = assembly.GetType(weaverTypeName);

                if (weaverType == null) return null;
                return Activator.CreateInstance(weaverType);
            }

            const string assemblyName = "AutoDI.Build";

            var weaverInstance = (Task)ProcessAssembly(Assembly.Load(assemblyName));
            if (weaverInstance != null)
                return new Weaver(weaverTypeName, weaverInstance);

            throw new Exception($"Failed to find weaver task '{weaverTypeName}'. Could not locate {weaverTypeName} in {assemblyName}.");
        }

        public Task Instance { get; }
        public string Name { get; }
        public XElement Config { get; set; }

        internal Weaver(string name, Task taskInstance)
        {
            Instance = taskInstance ?? throw new ArgumentNullException(nameof(taskInstance));
            Name = name;
        }

        public void ApplyToAssembly(string assemblyFilePath)
        {
            dynamic task = Instance;
            var buildEngine = new InMemoryBuildEngine();
            task.AssemblyFile = assemblyFilePath;
            task.BuildEngine = buildEngine;
            if (Config != null)
            {
                //task.Config = Config;
            }
            bool result = task.Execute();

            if (buildEngine.LoggedErrors.Any())
            {
                throw new WeaverErrorException(buildEngine.LoggedErrors);
            }
            if (!result)
            {
                throw new Exception("Task did not succeed");
            }
        }

        private class InMemoryBuildEngine : IBuildEngine
        {
            private readonly List<string> _LoggedErrors = new List<string>();
            public IReadOnlyList<string> LoggedErrors => _LoggedErrors;
            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                _LoggedErrors.Add(e.Message);
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
