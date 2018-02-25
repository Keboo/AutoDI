using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AutoDI.Fody;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Mono.Cecil;

namespace AutoDI.MSBuild.CSharp
{
    public class Compiler : MarshalByRefObject, ICodeBuilder
    {
        static Compiler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                return null;
            };
        }

        public bool Execute(string projectPath)
        {
            try
            {
                using (var workspace = MSBuildWorkspace.Create())
                {
                    //try
                    //{
                    AssemblyDefinition compiledAssembly;
                    workspace.LoadMetadataForReferencedProjects = true;
                    Project project = workspace.OpenProjectAsync(projectPath).Result;
                    //var tree = project.Documents.First().GetSyntaxTreeAsync().Result;
                    //var fileParser = new FileParser();
                    //fileParser.Visit(tree.GetRoot());
                    foreach (var document in project.Documents)
                    {

                    }
                    
                    var compile = project.GetCompilationAsync().Result;
                    var errors = compile.GetDiagnostics();
                    foreach (var tree in compile.SyntaxTrees)
                    {
                        
                    }
                    using (var ms = new MemoryStream())
                    {
                        compile.Emit(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        compiledAssembly = AssemblyDefinition.ReadAssembly(ms);
                    }


                    XElement configElement = GetConfigElement(projectPath);
                    var settings = Settings.Parse(new Settings(), configElement);
                    if (settings.GenerateRegistrations)
                    {
                        var assemblyResolver = new DefaultAssemblyResolver();
                        var typeResolver = new TypeResolver(compiledAssembly.MainModule, assemblyResolver, Logger);
                        ICollection<TypeDefinition> allTypes =
                            typeResolver.GetAllTypes(settings, out AssemblyDefinition autoDIAssembly);
                        Mapping mapping = Mapping.GetMapping(settings, allTypes, Logger);

                    }
                    //
                    //    return true;
                    //}
                    //finally
                    //{
                    //    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;
                    //}
                }
            }
            catch (Exception e)
            {

            }

            return true;

            void Logger(string message, DebugLogLevel level)
            {
                //TODO: Actually log...
            }
        }

        private XElement GetConfigElement(string projectPath)
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            if (projectDir == null) return null;
            var configFile = Path.Combine(projectDir, "FodyWeavers.xml");
            if (File.Exists(configFile))
            {
                return XElement.Load(configFile);
            }
            return null;
        }
    }
}
