using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
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

        public void AddWeaver(string weaverName)
        {
            bool ProcessAssembly(Assembly assembly)
            {
                Type weaverType = assembly.GetType(WeaverName);
                if (weaverType == null) return false;
                _weavers.Add(Activator.CreateInstance(weaverType));
                return true;
            }

            string assemblyName = $"{weaverName}.Fody";

            try
            {
                if (ProcessAssembly(Assembly.Load(assemblyName)))
                    return;
            }
            catch
            {
                // ignored
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName == assemblyName))
            {
                if (ProcessAssembly(assembly))
                    return;
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
                            case "/*<code_file>*/":
                            case "//<code_file>":
                                include = true;
                                break;
                            case "/*</code_file>*/":
                            case "//</code_file>":
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
                    file.Position = 0;
                    var module = ModuleDefinition.ReadModule(file);
                    foreach (dynamic weaver in _weavers)
                    {
                        weaver.ModuleDefinition = module;
                        weaver.Execute();
                    }
                    file.Position = 0;
                    module.Write(file);
                }
            }

            var assembly = Assembly.Load(File.ReadAllBytes(filePath));
            //File.Delete(filePath);
            return assembly;
        }
    }

    public enum AssemblyType
    {
        ConsoleApplication = OutputKind.ConsoleApplication,
        WindowsApplication = OutputKind.WindowsApplication,
        DynamicallyLinkedLibrary = OutputKind.DynamicallyLinkedLibrary,
        NetModule = OutputKind.NetModule
    }
}
