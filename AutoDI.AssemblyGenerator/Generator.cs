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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace AutoDI.AssemblyGenerator
{
    public class Generator
    {
        private static int _instanceCount = 1;
        private readonly AssemblyType _assebmlyType;
        private readonly string _assemblyName = $"AssemblyToTest{_instanceCount++}";
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
            catch
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
            throw new Exception($"Could not find '{weaverName}' weaver");
        }

        public void AddReference(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new ArgumentException($"Could not find file '{filePath}'");
            _references.Add(MetadataReference.CreateFromFile(filePath));
        }

        public async Task<Assembly> Execute([CallerFilePath] string sourceFile = null)
        {
            if (sourceFile == null) throw new ArgumentNullException(nameof(sourceFile));

            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();

            async Task<DocumentInfo> GetDocument()
            {
                var sb = new StringBuilder();
                using (var sr = new StreamReader(sourceFile))
                {
                    bool include = false;
                    while (!sr.EndOfStream)
                    {
                        string line = await sr.ReadLineAsync();
                        switch (line.Trim())
                        {
                            case "/*<gen>*/":
                            case "//<gen>":
                                include = true;
                                break;
                            case "/*</gen>*/":
                            case "//</gen>":
                                include = false;
                                break;
                            default:
                                if (include)
                                {
                                    sb.AppendLine(line);
                                }
                                break;
                        }
                    }
                }

                var rv = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "Generated.cs",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(sb.ToString()), VersionStamp.Create())));
                return rv;
            }


            var documents = new List<DocumentInfo>
            {
                await GetDocument()
            };

            var project = workspace.AddProject(ProjectInfo.Create(projectId,
                VersionStamp.Create(), _assemblyName, _assemblyName, LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions((OutputKind)_assebmlyType),
                documents: documents, metadataReferences: _references, 
                filePath: Path.GetFullPath($"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.csproj")));


            var compile = await project.GetCompilationAsync();
            string filePath = $"{_assemblyName}-{Path.GetFileNameWithoutExtension(sourceFile)}.dll";
            using (var file = File.Create(filePath))
            {
                var emitResult = compile.Emit(file);
                if (emitResult.Success)
                {
                    //ms.Position = 0;
                    
                    foreach (dynamic weaver in _weavers)
                    {
                        file.Position = 0;
                        var module = ModuleDefinition.ReadModule(file);
                        weaver.ModuleDefinition = module;
                        var errors = new StringBuilder();
                        weaver.LogError = new Action<string>(s =>
                        {
                            errors.AppendLine(s);
                        });
                        weaver.LogWarning = new Action<string>(s => Debug.WriteLine($"Weaver Warning: {s}"));
                        weaver.LogInfo = new Action<string>(s => Debug.WriteLine($"Weaver Info: {s}"));
                        weaver.LogDebug = new Action<string>(s => Debug.WriteLine($"Weaver Debug: {s}"));
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

            var assembly = Assembly.Load(File.ReadAllBytes(filePath));
            //File.Delete(filePath);
            return assembly;
        }
    }
}
