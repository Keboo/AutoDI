using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Tasks;

namespace AutoDI.AssemblyGenerator
{
    public class Gen
    {
        private readonly List<Type> _types = new List<Type>();
        private readonly List<object> _weavers = new List<object>();

        public Gen()
        {

        }

        public void AddWeaver<T>() where T : class
        {
            if (typeof(T).Name != "ModuleWeaver")
                throw new InvalidOperationException();

            _weavers.Add(Activator.CreateInstance<T>());
        }

        public void AddType<T>()
        {
            _types.Add(typeof(T));
        }

        public async Task Execute(Action test, [CallerFilePath] string sourceFile = null)
        {

            using (var sr = new StreamReader(sourceFile))
            using (var sw = new StreamWriter("test.cs"))
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
                                await sw.WriteLineAsync(line);
                            }
                            break;
                    }
                }
            }

            var project = new Project();
            project.SetProperty("TargetFramework", "net461");

            project.AddItem("Compile", Path.GetFileName("test.cs"));
            project.Save("MyProj.csproj");
            bool buildResult = project.Build(new Logger());

        }

        private class Logger : ILogger
        {
            public void Initialize(IEventSource eventSource)
            {
                eventSource.AnyEventRaised += EventSourceOnAnyEventRaised;
            }

            private void EventSourceOnAnyEventRaised(object sender, BuildEventArgs e)
            {
                Debug.WriteLine($"   => {e.Message}");
            }

            public void Shutdown()
            {
                
            }

            public LoggerVerbosity Verbosity { get; set; }
            public string Parameters { get; set; }
        }
    }
}
