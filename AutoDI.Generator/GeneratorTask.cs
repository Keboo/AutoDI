using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AutoDI.Fody;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using ILogger = AutoDI.Fody.ILogger;

namespace AutoDI.Generator
{
    public class GeneratorTask : Task, ICancelableTask
    {
        [Required]
        public string ProjectPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string GeneratedFilePath { get; set; }

        private ITaskItem[] _generatedCodeFiles;
        /// <summary>Gets or sets the list of generated managed code files.</summary>
        /// <returns>The list of generated managed code files.</returns>
        [Output]
        public ITaskItem[] GeneratedCodeFiles
        {
            get => _generatedCodeFiles ?? new ITaskItem[0];
            set => _generatedCodeFiles = value;
        }

        public override bool Execute()
        {
            XElement configElement = GetConfigElement(ProjectPath);
            var settings = Settings.Parse(new Settings(), configElement);
            var logger = new TaskLogger(BuildEngine, settings.DebugLogLevel);
            if (settings.GenerateRegistrations)
            {
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(OutputPath));

                var compiledAssembly = AssemblyDefinition.ReadAssembly(OutputPath);
                var typeResolver = new TypeResolver(compiledAssembly.MainModule, assemblyResolver, logger);
                ICollection<TypeDefinition> allTypes =
                    typeResolver.GetAllTypes(settings, out AssemblyDefinition _);
                Mapping mapping = Mapping.GetMapping(settings, allTypes, logger);

                Directory.CreateDirectory(Path.GetDirectoryName(GeneratedFilePath));
                using (var file = File.Open(GeneratedFilePath, FileMode.Create))
                {
                    WriteClass(mapping, SetupMethod.Find(compiledAssembly.MainModule, logger), file);
                    GeneratedCodeFiles = new ITaskItem[] { new TaskItem(GeneratedFilePath) };
                }
            }
            return true;
        }

        private static XElement GetConfigElement(string projectPath)
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            if (projectDir == null) return null;
            var configFile = Path.Combine(projectDir, "FodyWeavers.xml");
            if (File.Exists(configFile))
            {
                var xElement = XElement.Load(configFile);
                return xElement.Elements("AutoDI").FirstOrDefault();
            }
            return null;
        }

        private static void WriteClass(Mapping mapping, MethodDefinition setupMethod, Stream output)
        {
            using (var sw = new StreamWriter(output))
            {
                sw.WriteLine("using System;");
                sw.WriteLine("using AutoDI;");
                sw.WriteLine("using Microsoft.Extensions.DependencyInjection;");
                sw.WriteLine("namespace AutoDI.Generated");
                sw.WriteLine("{");
                sw.WriteLine("    public static partial class AutoDI");
                sw.WriteLine("    {");

                int index = 0;
                foreach (TypeMap typeMap in mapping)
                {
                    if (!typeMap.TargetType.CanMapType()) continue;
                    MethodDefinition ctor = typeMap.TargetType.GetMappingConstructor();
                    if (ctor == null) continue;

                    sw.WriteLine($"        private static global::{typeMap.TargetType.FullNameCSharp()} generated_{index++}(IServiceProvider serviceProvider)");
                    sw.WriteLine("        {");
                    sw.Write($"            return new global::{typeMap.TargetType.FullNameCSharp()}(");

                    sw.Write(string.Join(", ", ctor.Parameters.Select(p => $"serviceProvider.GetService<global::{p.ParameterType.FullNameCSharp()}>()")));

                    sw.WriteLine(");");
                    sw.WriteLine("        }");
                }

                sw.WriteLine("        private static void AddServices(IServiceCollection collection)");
                sw.WriteLine("        {");
                index = 0;
                foreach (TypeMap typeMap in mapping)
                {
                    if (!typeMap.TargetType.CanMapType() || typeMap.TargetType.GetMappingConstructor() == null) continue;
                    foreach (TypeLifetime lifetime in typeMap.Lifetimes)
                    {
                        sw.WriteLine($"            collection.AddAutoDIService<global::{typeMap.TargetType.FullNameCSharp()}>(generated_{index}, new System.Type[{lifetime.Keys.Count}]");
                        sw.WriteLine("            {");
                        sw.Write("                ");
                        sw.WriteLine(string.Join(", ", lifetime.Keys.Select(t => $"typeof(global::{t.FullNameCSharp()})")));
                        sw.WriteLine($"            }}, Lifetime.{lifetime.Lifetime});");
                    }
                    index++;
                }
                sw.WriteLine("        }");

                sw.WriteLine("        static partial void DoInit(Action<IApplicationBuilder> configure)");
                sw.WriteLine("        {");
                sw.WriteLine("            if (_globalServiceProvider != null)");
                sw.WriteLine("            {");
                sw.WriteLine("                throw new AlreadyInitializedException();");
                sw.WriteLine("            }");
                sw.WriteLine("            IApplicationBuilder applicationBuilder = new ApplicationBuilder();");
                sw.WriteLine("            applicationBuilder.ConfigureServices(AddServices);");
                if (setupMethod != null)
                {
                    sw.WriteLine($"            global::{setupMethod.DeclaringType.FullNameCSharp()}.{setupMethod.Name}(applicationBuilder);");
                }
                sw.WriteLine("            if (configure != null)");
                sw.WriteLine("            {");
                sw.WriteLine("                configure(applicationBuilder);");
                sw.WriteLine("            }");
                sw.WriteLine("            _globalServiceProvider = applicationBuilder.Build();");
                sw.WriteLine("            GlobalDI.Register(_globalServiceProvider);");
                sw.WriteLine("        }");

                sw.WriteLine("        static partial void DoDispose()");
                sw.WriteLine("        {");
                sw.WriteLine("            IDisposable disposable;");
                sw.WriteLine("            if ((disposable = (_globalServiceProvider as IDisposable)) != null)");
                sw.WriteLine("            {");
                sw.WriteLine("                disposable.Dispose();");
                sw.WriteLine("            }");
                sw.WriteLine("            GlobalDI.Unregister(_globalServiceProvider);");
                sw.WriteLine("            _globalServiceProvider = null;");
                sw.WriteLine("        }");


                sw.WriteLine("        private static IServiceProvider _globalServiceProvider;");

                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
        }



        public void Cancel()
        {

        }

        private class TaskLogger : ILogger
        {
            private const string Sender = "AutoDI";
            private readonly IBuildEngine _BuildEngine;
            private readonly DebugLogLevel _DebugLogLevel;

            public TaskLogger(IBuildEngine buildEngine, DebugLogLevel debugLogLevel)
            {
                _BuildEngine = buildEngine;
                _DebugLogLevel = debugLogLevel;
            }

            public void Debug(string message, DebugLogLevel debugLevel)
            {
                if (debugLevel <= _DebugLogLevel)
                {
                    _BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, "", "AutoDI", MessageImportance.Normal));
                }
            }

            public void Info(string message)
            {
                _BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, "", "AutoDI", MessageImportance.High));
            }

            public void Warning(string message)
            {
                _BuildEngine.LogWarningEvent(new BuildWarningEventArgs("", "", null, 0, 0, 0, 0, message, "", Sender));
            }

            public void Error(string message)
            {
                _BuildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", null, 0, 0, 0, 0, message, "", Sender));
            }
        }
    }
}
