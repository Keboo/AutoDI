using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace AutoDI.AssemblyGenerator
{
    public class Generator
    {
        private static int _instanceCount = 1;
        private readonly AssemblyType _assebmlyType;
        //private readonly string _assemblyName = $"AssemblyToTest{_instanceCount++}";
        private const string WeaverName = "ModuleWeaver";

        private readonly List<object> _weavers = new List<object>();
        private readonly List<MetadataReference> _references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        public Generator(AssemblyType assebmlyType = AssemblyType.DynamicallyLinkedLibrary)
        {
            _assebmlyType = assebmlyType;
        }

        public object AddWeaver(string weaverName)
        {
            object ProcessAssembly(Assembly assembly)
            {
                Type weaverType = assembly.GetType(WeaverName);
                if (weaverType == null) return null;
                object weaver = Activator.CreateInstance(weaverType);
                _weavers.Add(weaver);
                return weaver;
            }

            string assemblyName = $"{weaverName}.Fody";

            try
            {
                object weaver = ProcessAssembly(Assembly.Load(assemblyName));
                if (weaver != null)
                    return weaver;
            }
            catch (Exception ex)
            {
                // ignored
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName == assemblyName))
            {
                object weaver = ProcessAssembly(assembly);
                if (weaver != null)
                    return weaver;
            }
            throw new Exception($"Failed to add weaver '{weaverName}'. Could not locate {weaverName}.Fody assembly.");
        }

        public void AddReference(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new ArgumentException($"Could not find file '{filePath}'");
            _references.Add(MetadataReference.CreateFromFile(filePath));
        }

        public async Task<Assembly> Execute([CallerFilePath] string sourceFile = null)
        {
            return null;
        }

        public async Task<Dictionary<string, Assembly>> Execute2([CallerFilePath] string sourceFile = null)
        {
            if (sourceFile == null) throw new ArgumentNullException(nameof(sourceFile));

            var builtAssemblies = new Dictionary<string, Assembly>();

            IEnumerable<AssemblyInfo> GetAssemblies()
            {
                var assemblyRegex = new Regex(@"<assembly(:\s*(?<Name>\w+))?\s*/>");
                var typeRegex = new Regex(@"<type:\s*(?<Name>\w+)\s*/>");
                var referenceRegex = new Regex(@"<ref:\s*(?<Name>\w+)\s*/>");
                var weaverRegex = new Regex(@"<weaver:\s*(?<Name>[\w_\.]+)\s*/>");

                using (var sr = new StreamReader(sourceFile))
                {
                    AssemblyInfo currentAssembly = null;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line == null) continue;
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                        {
                            // ReSharper disable TooWideLocalVariableScope
                            Match assemblyStartMatch, typeMatch, referenceMatch, weaverMatch;
                            // ReSharper restore TooWideLocalVariableScope
                            if ((assemblyStartMatch = assemblyRegex.Match(trimmed)).Success)
                            {
                                if (currentAssembly != null)
                                    yield return currentAssembly;
                                currentAssembly = new AssemblyInfo(assemblyStartMatch.Groups["Name"]?.Value);
                            }
                            else if (currentAssembly != null)
                            {
                                if ((typeMatch = typeRegex.Match(trimmed)).Success)
                                {
                                    if (Enum.TryParse(typeMatch.Groups["Name"].Value, true, out OutputKind output))
                                    {
                                        currentAssembly.OutputKind = output;
                                    }
                                }
                                else if ((referenceMatch = referenceRegex.Match(trimmed)).Success)
                                {
                                    MetadataReference GetReference(string name)
                                    {
                                        if (builtAssemblies.TryGetValue(name, out Assembly builtAssembly))
                                        {
                                            return MetadataReference.CreateFromFile(builtAssembly.Location);
                                        }
                                        string filePath = $@".\{name}.dll";
                                        if (File.Exists(filePath))
                                        {
                                            return MetadataReference.CreateFromFile(filePath);
                                        }
                                        //Assembly loadedAssembly = Assembly.Load(new AssemblyName(name));
                                        //if (loadedAssembly != null)
                                        //{
                                        //    return MetadataReference.CreateFromFile(loadedAssembly.Location);
                                        //}
                                        return null;
                                    }

                                    MetadataReference reference = GetReference(referenceMatch.Groups["Name"].Value);
                                    if (reference != null)
                                    {
                                        currentAssembly.AddReference(reference);
                                    }
                                    //TODO: Else
                                }
                                else if ((weaverMatch = weaverRegex.Match(trimmed)).Success)
                                {
                                    object GetWeaver(string weaverName)
                                    {
                                        object ProcessAssembly(Assembly assembly)
                                        {
                                            Type weaverType = assembly.GetType(WeaverName);
                                            if (weaverType == null) return null;
                                            object weaverInstance = Activator.CreateInstance(weaverType);
                                            _weavers.Add(weaverInstance);
                                            return weaverInstance;
                                        }

                                        string assemblyName = $"{weaverName}.Fody";

                                        try
                                        {
                                            object weaverInstance = ProcessAssembly(Assembly.Load(assemblyName));
                                            if (weaverInstance != null)
                                                return weaverInstance;
                                        }
                                        catch (Exception ex)
                                        {
                                            // ignored
                                        }
                                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                                            .Where(a => a.FullName == assemblyName))
                                        {
                                            object weaverInstance = ProcessAssembly(assembly);
                                            if (weaverInstance != null)
                                                return weaverInstance;
                                        }
                                        throw new Exception($"Failed to add weaver '{weaverName}'. Could not locate {weaverName}.Fody assembly.");
                                    }

                                    object weaver = GetWeaver(weaverMatch.Groups["Name"].Value);
                                    if (weaver != null)
                                    {
                                        currentAssembly.AddWeaver(weaver);
                                    }
                                    //TODO: Else
                                }
                            }
                        }
                        currentAssembly?.AppendLine(line);
                    }
                    if (currentAssembly != null)
                        yield return currentAssembly;
                }
            }

            var workspace = new AdhocWorkspace();

            foreach (AssemblyInfo assemblyInfo in GetAssemblies())
            {
                string assemblyName = $"AssemblyToTest{_instanceCount++}";

                var projectId = ProjectId.CreateNewId();

                var document = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "Generated.cs",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(assemblyInfo.GetContents()), VersionStamp.Create())));

                var project = workspace.AddProject(ProjectInfo.Create(projectId,
                    VersionStamp.Create(), assemblyName, assemblyName, LanguageNames.CSharp,
                    compilationOptions: new CSharpCompilationOptions(assemblyInfo.OutputKind),
                    documents: new[] { document }, metadataReferences: assemblyInfo.References,
                    filePath: Path.GetFullPath($"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.csproj")));

                Compilation compile = await project.GetCompilationAsync();
                string filePath = Path.GetFullPath($"{assemblyName}.dll");
                using (var file = File.Create(filePath))
                {
                    var emitResult = compile.Emit(file);
                    if (emitResult.Success)
                    {
                        //ms.Position = 0;
                        var assemblyResolver = new DefaultAssemblyResolver();
                        foreach (dynamic weaver in assemblyInfo.Weavers)
                        {
                            file.Position = 0;
                            var module = ModuleDefinition.ReadModule(file);
                            weaver.ModuleDefinition = module;
                            var errors = new StringBuilder();
                            weaver.LogError = new Action<string>(s =>
                            {
                                errors.AppendLine(s);
                            });
                            weaver.LogWarning = new Action<string>(s => Debug.WriteLine($" Warning: {s}"));
                            weaver.LogInfo = new Action<string>(s => Debug.WriteLine($" Info: {s}"));
                            weaver.LogDebug = new Action<string>(s => Debug.WriteLine($" Debug: {s}"));
                            weaver.AssemblyResolver = assemblyResolver;
                            weaver.Execute();
                            file.Position = 0;
                            module.Write(file);
                            if (errors.Length > 0)
                            {
                                throw new Exception($"Weaver Error {errors}");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(string.Join(Environment.NewLine, emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.GetMessage())));
                    }
                }
                builtAssemblies.Add(assemblyInfo.Name ?? assemblyName, Assembly.LoadFile(filePath));
            }
            return builtAssemblies;
        }

        private class AssemblyInfo
        {
            private readonly StringBuilder _contents = new StringBuilder();
            private readonly List<object> _weavers = new List<object>();
            private readonly List<MetadataReference> _references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            public string Name { get; }

            public IReadOnlyList<MetadataReference> References => _references;

            public IReadOnlyList<object> Weavers => _weavers;

            public OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;

            public AssemblyInfo(string name)
            {
                Name = name;
            }

            public void AppendLine(string line) => _contents.AppendLine(line);

            public void AddReference(MetadataReference reference) => _references.Add(reference);

            public void AddWeaver(object weaver) => _weavers.Add(weaver);

            public string GetContents() => _contents.ToString();
        }
    }
}
