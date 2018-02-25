
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
                    WriteClass(mapping, file);
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

        private static void WriteClass(Mapping mapping, Stream output)
        {
            using (var sw = new StreamWriter(output))
            {
                foreach (string @namespace in GetAllNamespaces().Distinct().OrderBy(x => x))
                {
                    sw.WriteLine($"using {@namespace};");
                }
                sw.WriteLine("namespace AutoDI.Generated");
                sw.WriteLine("{");
                sw.WriteLine("    public static class AutoDI");
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

                sw.WriteLine("        public static void AddServices(IServiceCollection collection)");
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

                sw.WriteLine("    }");
                sw.WriteLine("}");
            }

            IEnumerable<string> GetAllNamespaces()
            {
                yield return "System";
                yield return "Microsoft.Extensions.DependencyInjection";
            }
        }

        //public override bool Execute()
        //{
        //    string myLocation = @"C:\Dev\AutoDI\AutoDI.MSBuild.CSharp\bin\Debug\net461\AutoDI.MSBuild.CSharp.dll";
        //    string path = Path.GetDirectoryName(myLocation);
        //    var appDomainSetup = new AppDomainSetup
        //    {
        //        ApplicationName = Guid.NewGuid().ToString("N"),
        //        ApplicationBase = path,
        //        ConfigurationFile = $"{myLocation}.config"
        //    };
        //    var appDomain = AppDomain.CreateDomain("MyTestDomain", null, appDomainSetup);
        //    //appDomain.AssemblyResolve += (sender, args) =>
        //    //{
        //    //    return null;
        //    //    //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //    //    //var rv = assemblies.FirstOrDefault(x => string.Equals(x.FullName, args.Name, StringComparison.OrdinalIgnoreCase));
        //    //    //return rv;
        //    //};
        //
        //    Assembly OnCurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        //    {
        //        var rv = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => string.Equals(x.FullName, args.Name, StringComparison.OrdinalIgnoreCase));
        //
        //        return rv;
        //    }
        //
        //    var assembly = Assembly.LoadFile(myLocation);
        //    var builderTypes = assembly.GetExportedTypes().ToList();
        //
        //
        //    AppDomain.CurrentDomain.AssemblyResolve += OnCurrentDomainOnAssemblyResolve;
        //    foreach (Type builderType in builderTypes)
        //    {
        //        var executeMethod = builderType.GetMethods().FirstOrDefault(m => m.Name == "Execute");
        //        var builder = appDomain.CreateInstanceFromAndUnwrap(myLocation, builderType.FullName);
        //        //object builder = appDomain.CreateInstanceAndUnwrap("AutoDI.MSBuild.CSharp", builderType.FullName);
        //        executeMethod.Invoke(builder, new object[] {ProjectPath});
        //    }
        //    
        //    return true;
        //}

        public void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
