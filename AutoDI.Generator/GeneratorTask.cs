using AutoDI.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoDI.Generator
{
    public class GeneratorTask : AssemblyRewriteTask
    {
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

        protected override bool WeaveAssembly()
        {
            var settings = Settings.Load(ModuleDefinition);

            if (settings.GenerateRegistrations)
            {
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(OutputPath));

                var logger = new TaskLogger(this) { DebugLogLevel = settings.DebugLogLevel };

                var typeResolver = new TypeResolver(ModuleDefinition, assemblyResolver, logger);

                ICollection<TypeDefinition> allTypes =
                    typeResolver.GetAllTypes(settings, out AssemblyDefinition _);
                Mapping mapping = Mapping.GetMapping(settings, allTypes, logger);

                if (Path.GetDirectoryName(GeneratedFilePath) is string directory)
                {
                    Directory.CreateDirectory(directory);
                }
                using (var file = File.Open(GeneratedFilePath, FileMode.Create))
                {
                    WriteClass(mapping, settings, SetupMethod.Find(ModuleDefinition, logger), file);
                    GeneratedCodeFiles = new ITaskItem[] { new TaskItem(GeneratedFilePath) };
                }
            }
            //Returning false ensures that the original module is not re-written with any changes
            return false;
        }

        private static void WriteClass(Mapping mapping, Settings settings, MethodDefinition setupMethod, Stream output)
        {
            const string @namespace = "AutoDI.Generated";
            const string className = "AutoDI";
            using (var sw = new StreamWriter(output))
            {
                sw.WriteLine(0, "using System;");
                sw.WriteLine(0, "using AutoDI;");
                sw.WriteLine(0, "using Microsoft.Extensions.DependencyInjection;");
                sw.WriteLine(0, $"namespace {@namespace}");
                sw.WriteLine(0, "{");
                sw.WriteLine(1, $"public static partial class {className}");
                sw.WriteLine(1, "{");

                int index = 0;
                var factoryMethodIndexes = new Dictionary<string, int>();

                foreach (Registration registration in mapping)
                {
                    if (!registration.TargetType.CanMapType()) continue;
                    if (factoryMethodIndexes.ContainsKey(registration.TargetType.FullName)) continue;

                    MethodDefinition ctor = registration.TargetType.GetMappingConstructor();
                    if (ctor == null) continue;

                    factoryMethodIndexes[registration.TargetType.FullName] = index;

                    sw.WriteLine(2, $"private static global::{registration.TargetType.FullNameCSharp()} generated_{index++}(IServiceProvider serviceProvider)");
                    sw.WriteLine(2, "{");
                    sw.Write(3, $"return new global::{registration.TargetType.FullNameCSharp()}(");

                    sw.Write(string.Join(", ", ctor.Parameters.Select(p => $"serviceProvider.GetService<global::{p.ParameterType.FullNameCSharp()}>()")));

                    sw.WriteLine(");");
                    sw.WriteLine(2, "}");
                }

                sw.WriteLine(2, "private static void AddServices(IServiceCollection collection)");
                sw.WriteLine(2, "{");
                if (settings.DebugExceptions)
                {
                    sw.WriteLine(3, "List<Exception> list = new List<Exception>();");
                }

                foreach (Registration registration in mapping)
                {
                    if (!registration.TargetType.CanMapType() || registration.TargetType.GetMappingConstructor() == null) continue;

                    int indent = 3;
                    if (settings.DebugExceptions)
                    {
                        sw.WriteLine(indent, "try");
                        sw.WriteLine(indent++, "{");
                    }

                    sw.WriteLine(indent, $"collection.AddAutoDIService(typeof(global::{registration.Key.FullNameCSharp()}),typeof(global::{registration.TargetType.FullNameCSharp()}), generated_{factoryMethodIndexes[registration.TargetType.FullName]}, Lifetime.{registration.Lifetime});");
                    
                    if (settings.DebugExceptions)
                    {
                        sw.WriteLine(--indent, "}");
                        sw.WriteLine(indent, "catch(Exception innerException)");
                        sw.WriteLine(indent, "{");
                        sw.WriteLine(indent + 1, $"list.Add(new AutoDIException(\"Error adding type '{registration.TargetType.FullNameCSharp()}' with key '{registration.Key.FullNameCSharp()}'\", innerException));");
                        sw.WriteLine(indent, "}");
                    }
                }

                if (settings.DebugExceptions)
                {
                    sw.WriteLine(3, "if (list.Count > 0)");
                    sw.WriteLine(3, "{");
                    sw.WriteLine(4, $"throw new AggregateException(\"Error in {@namespace}.{className}.AddServices() generated method\", list);");
                    sw.WriteLine(3, "}");
                }
                sw.WriteLine(2, "}");

                sw.WriteLine(2, "static partial void DoInit(Action<IApplicationBuilder> configure)");
                sw.WriteLine(2, "{");
                sw.WriteLine(3, "if (_globalServiceProvider != null)");
                sw.WriteLine(3, "{");
                sw.WriteLine(4, "throw new AlreadyInitializedException();");
                sw.WriteLine(3, "}");
                sw.WriteLine(3, "IApplicationBuilder applicationBuilder = new ApplicationBuilder();");
                sw.WriteLine(3, "applicationBuilder.ConfigureServices(AddServices);");
                if (setupMethod != null)
                {
                    sw.WriteLine(3, $"global::{setupMethod.DeclaringType.FullNameCSharp()}.{setupMethod.Name}(applicationBuilder);");
                }
                sw.WriteLine(3, "if (configure != null)");
                sw.WriteLine(3, "{");
                sw.WriteLine(4, "configure(applicationBuilder);");
                sw.WriteLine(3, "}");
                sw.WriteLine(3, "_globalServiceProvider = applicationBuilder.Build();");
                sw.WriteLine(3, "GlobalDI.Register(_globalServiceProvider);");
                sw.WriteLine(2, "}");

                sw.WriteLine(2, "static partial void DoDispose()");
                sw.WriteLine(2, "{");
                sw.WriteLine(3, "IDisposable disposable;");
                sw.WriteLine(3, "if ((disposable = (_globalServiceProvider as IDisposable)) != null)");
                sw.WriteLine(3, "{");
                sw.WriteLine(4, "disposable.Dispose();");
                sw.WriteLine(3, "}");
                sw.WriteLine(3, "GlobalDI.Unregister(_globalServiceProvider);");
                sw.WriteLine(3, "_globalServiceProvider = null;");
                sw.WriteLine(2, "}");


                sw.WriteLine(2, "private static IServiceProvider _globalServiceProvider;");

                sw.WriteLine(1, "}");
                sw.WriteLine(0, "}");
            }
        }


    }
}
