
using AutoDI.Fody;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AutoDI.MSBuild
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

        //[Output]
        //public ITaskItem[] RemoveItems { get; set; }
        //
        //[Output]
        //public ITaskItem[] NewItems { get; set; }

        public override bool Execute()
        {
            XElement configElement = GetConfigElement(ProjectPath);
            var settings = Settings.Parse(new Settings(), configElement);
            if (settings.GenerateRegistrations)
            {
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(OutputPath));

                var compiledAssembly = AssemblyDefinition.ReadAssembly(OutputPath);
                var typeResolver = new TypeResolver(compiledAssembly.MainModule, assemblyResolver, Logger);
                ICollection<TypeDefinition> allTypes =
                    typeResolver.GetAllTypes(settings, out AssemblyDefinition autoDIAssembly);
                Mapping mapping = Mapping.GetMapping(settings, allTypes, Logger);

                Directory.CreateDirectory(Path.GetDirectoryName(GeneratedFilePath));
                using (var file = File.Open(GeneratedFilePath, FileMode.Create))
                {
                    //TODO: Logging
                    WriteClass(mapping, SetupMethod.Find(compiledAssembly.MainModule, null), file);
                    GeneratedCodeFiles = new ITaskItem[] { new TaskItem(GeneratedFilePath) };
                }
            }
            return true;

            void Logger(string message, DebugLogLevel level)
            {
                //TODO: Actually log...
            }
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
                sw.WriteLine("System");
                sw.WriteLine("AutoDI");
                sw.WriteLine("Microsoft.Extensions.DependencyInjection");
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

                    sw.WriteLine($"        private static {typeMap.TargetType.FullNameCSharp()} generated_{index++}(IServiceProvider serviceProvider)");
                    sw.WriteLine("        {");
                    sw.Write($"            return new {typeMap.TargetType.FullNameCSharp()}(");

                    sw.Write(string.Join(", ", ctor.Parameters.Select(p => $"serviceProvider.GetService<{p.ParameterType.FullNameCSharp()}>()")));

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
                        sw.WriteLine($"            collection.AddAutoDIService<{typeMap.TargetType.FullNameCSharp()}>(generated_{index}, new System.Type[{lifetime.Keys.Count}]");
                        sw.WriteLine("            {");
                        sw.Write("                ");
                        sw.WriteLine(string.Join(", ", lifetime.Keys.Select(t => $"typeof({t.FullNameCSharp()})")));
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
                    sw.WriteLine($"            {setupMethod.DeclaringType.FullNameCSharp()}.{setupMethod.Name}(applicationBuilder);");
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
    }
}
