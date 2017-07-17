using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoDI.AssemblyGenerator
{
    public class Generator
    {
        public event EventHandler<WeaverAddedEventArgs> WeaverAdded;

        private static int _instanceCount = 1;

        public async Task<Dictionary<string, AssemblyInfo>> Execute([CallerFilePath] string sourceFile = null)
        {
            if (sourceFile == null) throw new ArgumentNullException(nameof(sourceFile));

            var builtAssemblies = new Dictionary<string, AssemblyInfo>();

            IEnumerable<AssemblyInfo> GetAssemblies()
            {

                var assemblyRegex = new Regex(@"<\s*assembly(:\s*(?<Name>\w+))?\s*/?>");
                var endAssemblyRegex = new Regex(@"</\s*assembly\s*>");
                var typeRegex = new Regex(@"<\s*type:\s*(?<Name>\w+)\s*/>");
                var referenceRegex = new Regex(@"<\s*ref:\s*(?<Name>[\w_\.]+)\s*/>");
                var weaverRegex = new Regex(@"<\s*weaver:\s*(?<Name>[\w_\.]+)\s*/>");

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
                                if (endAssemblyRegex.IsMatch(trimmed))
                                {
                                    yield return currentAssembly;
                                    currentAssembly = null;
                                }
                                else if ((typeMatch = typeRegex.Match(trimmed)).Success)
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
                                        if (builtAssemblies.TryGetValue(name, out AssemblyInfo builtAssembly))
                                        {
                                            return MetadataReference.CreateFromFile(builtAssembly.Assembly.Location);
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
                                    Weaver weaver = Weaver.FindWeaver(weaverMatch.Groups["Name"].Value);
                                    if (weaver != null)
                                    {
                                        WeaverAdded?.Invoke(this, new WeaverAddedEventArgs(weaver));
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
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(assemblyInfo.GetContents()),
                        VersionStamp.Create())));

                var project = workspace.AddProject(ProjectInfo.Create(projectId,
                    VersionStamp.Create(), assemblyName, assemblyName, LanguageNames.CSharp,
                    compilationOptions: new CSharpCompilationOptions(assemblyInfo.OutputKind),
                    documents: new[] { document }, metadataReferences: assemblyInfo.References,
                    filePath: Path.GetFullPath(
                        $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.csproj")));

                Compilation compile = await project.GetCompilationAsync();
                string filePath = Path.GetFullPath($"{assemblyName}.dll");
                using (var file = File.Create(filePath))
                {
                    var emitResult = compile.Emit(file);
                    if (emitResult.Success)
                    {
                        foreach (Weaver weaver in assemblyInfo.Weavers)
                        {
                            file.Position = 0;
                            weaver.ApplyToAssembly(file);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Join(Environment.NewLine,
                            emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                                .Select(d => d.GetMessage())));
                    }
                }
                assemblyInfo.Assembly = Assembly.LoadFile(filePath);
                builtAssemblies.Add(assemblyInfo.Name ?? assemblyName, assemblyInfo);
            }
            return builtAssemblies;
        }
    }
}
