﻿using System.Collections;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace AutoDI.AssemblyGenerator;

public sealed class Weaver
{
    public static Weaver FindWeaver(string weaverTypeName)
    {
        object? ProcessAssembly(Assembly assembly)
        {
            //TODO: Check for BaseModuleWeaver type
            Type weaverType = assembly.GetType(weaverTypeName);

            return weaverType is null ? null : Activator.CreateInstance(weaverType);
        }

        const string assemblyName = "AutoDI.Build";

        var weaverInstance = (Task?)ProcessAssembly(Assembly.Load(assemblyName));
        return weaverInstance != null
            ? new Weaver(weaverTypeName, weaverInstance)
            : throw new Exception($"Failed to find weaver task '{weaverTypeName}'. Could not locate {weaverTypeName} in {assemblyName}.");
    }

    public Task Instance { get; }
    public string Name { get; }

    internal Weaver(string name, Task taskInstance)
    {
        Instance = taskInstance ?? throw new ArgumentNullException(nameof(taskInstance));
        Name = name;
    }

    public void ApplyToAssembly(string assemblyFilePath)
    {
        Task task = Instance;
        var buildEngine = new InMemoryBuildEngine();
        SetProperty("AssemblyFile", assemblyFilePath);
        task.BuildEngine = buildEngine;

        bool result = task.Execute();

        if (buildEngine.LoggedErrors.Any())
        {
            throw new WeaverErrorException(buildEngine.LoggedErrors);
        }
        if (!result)
        {
            throw new Exception("Task did not succeed");
        }

        void SetProperty<T>(string propertyName, T value)
        {
            task.GetType().GetProperty(propertyName)?.SetValue(task, value);
        }
    }

    private class InMemoryBuildEngine : IBuildEngine
    {
        private readonly List<string> _LoggedErrors = new();
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