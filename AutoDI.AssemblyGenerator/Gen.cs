using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace AutoDI.AssemblyGenerator
{
    public class Gen
    {
        //private readonly List<Type> _types = new List<Type>();
        //private readonly List<object> _weavers = new List<object>();
        //
        //public Gen()
        //{
        //
        //}
        //
        //public void AddWeaver<T>() where T : class
        //{
        //    if (typeof(T).Name != "ModuleWeaver")
        //        throw new InvalidOperationException();
        //
        //    _weavers.Add(Activator.CreateInstance<T>());
        //}
        //
        //public void AddType<T>()
        //{
        //    _types.Add(typeof(T));
        //}

        public async Task Execute(Action test, [CallerFilePath] string sourceFile = null)
        {
            var sb = new StringBuilder();
            using (var sr = new StreamReader(sourceFile))
            //using (var sw = new StreamWriter("test.cs"))
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
                                //await sw.WriteLineAsync(line);
                            }
                            break;
                    }
                }
            }
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();

            var doc = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "MyFile.cs",
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(sb.ToString()), VersionStamp.Create())));

            var @ref = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var project = workspace.AddProject(ProjectInfo.Create(projectId, 
                VersionStamp.Create(), "MyName", "MyAssembly", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary), 
                documents:new[] {doc}, metadataReferences:new [] {@ref}, filePath:Path.GetFullPath("TestFile.csproj")));
            

            var compile = await project.GetCompilationAsync();
            using (var file = File.Create("Foo.dll"))
            {
                var emitResult = compile.Emit(file);
                if (emitResult.Success)
                {
                    
                }
            }
        }

        //private class Logger : ILogger
        //{
        //    public void Initialize(IEventSource eventSource)
        //    {
        //        eventSource.AnyEventRaised += EventSourceOnAnyEventRaised;
        //    }
        //
        //    private void EventSourceOnAnyEventRaised(object sender, BuildEventArgs e)
        //    {
        //        Debug.WriteLine($"   => {e.Message}");
        //    }
        //
        //    public void Shutdown()
        //    {
        //        
        //    }
        //
        //    public LoggerVerbosity Verbosity { get; set; }
        //    public string Parameters { get; set; }
        //}
    }
}
