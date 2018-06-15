
using AutoDI.Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Linq;

// ReSharper disable once CheckNamespace
public partial class ModuleWeaver
{
    private Imports Import { get; set; }

    private void LoadRequiredData()
    {
        if (Import == null)
        {
            Import = new Imports(FindType, ModuleDefinition);
        }
    }

    internal class Imports
    {
        public Imports(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
        {
            System = new SystemImport(findType, moduleDefinition);
            AutoDI = new AutoDIImport(findType, moduleDefinition);
            DependencyInjection = new DependencyInjectionImport(findType, moduleDefinition);

            IServiceProvider = moduleDefinition.ImportReference(findType("System.IServiceProvider"));


            System_Func2_Ctor =
                moduleDefinition.ImportReference(findType("System.Func`2")).Resolve().GetConstructors().Single();

            System_Exception = moduleDefinition.ImportReference(findType("System.Exception"));

            List_Type = findType("System.Collections.Generic.List`1");

            var aggregateExceptionType = findType("System.AggregateException");
            var enumerableType = findType("System.Collections.Generic.IEnumerable`1");
            var enumerableException = enumerableType.MakeGenericInstanceType(System_Exception);

            System_AggregateException_Ctor = moduleDefinition.ImportReference(aggregateExceptionType
                .GetConstructors().Single(c =>
                    c.Parameters.Count == 2 &&
                    c.Parameters[0].ParameterType.IsType<string>() &&
                    c.Parameters[1].ParameterType.IsType(enumerableException)));
            
           
        }

        public SystemImport System { get; }

        public AutoDIImport AutoDI { get; }

        public DependencyInjectionImport DependencyInjection { get; }
        
        public class SystemImport
        {
            public SystemImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Action = new ActionImport(findType, moduleDefinition);
                Type = new TypeImport(findType, moduleDefinition);
            }

            public ActionImport Action { get; }

            public TypeImport Type { get; }

            public class ActionImport
            {
                public TypeReference Type { get; }

                public MethodReference Ctor { get; }

                public MethodReference Invoke { get; }

                public ActionImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    Type = moduleDefinition.ImportReference(findType("System.Action`1"));

                    var resolved = Type.Resolve();

                    Invoke = moduleDefinition.ImportReference(resolved.GetMethods().Single(x => x.Name == "Invoke"));
                    
                    Ctor = moduleDefinition.ImportReference(resolved.GetConstructors().Single());
                }
            }

            public class TypeImport
            {
                public TypeReference Type { get; }

                public MethodReference GetTypeFromHandle { get; }


                public TypeImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    var type = findType("System.Type");
                    Type = moduleDefinition.ImportReference(type);

                    GetTypeFromHandle =
                        moduleDefinition.ImportReference(type.GetMethods().Single(m => m.Name == "GetTypeFromHandle"));
                }
            }
        }

        public class AutoDIImport
        {
            public AutoDIExceptionsImport Exceptions { get; }

            public IApplicationBuilderImport IApplicationBuilder { get; }

            public ApplicationBuilderImport ApplicationBuilder { get; }

            public GlobalDIImport GlobalDI { get; }

            public ServiceCollectionMixinsImport ServiceCollectionMixins { get; }

            public TypeReference DependencyAttributeType { get; }

            public AutoDIImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                Exceptions = new AutoDIExceptionsImport(findType, moduleDefinition);
                IApplicationBuilder = new IApplicationBuilderImport(findType, moduleDefinition);
                ApplicationBuilder = new ApplicationBuilderImport(findType, moduleDefinition);
                GlobalDI = new GlobalDIImport(findType, moduleDefinition);
                ServiceCollectionMixins = new ServiceCollectionMixinsImport(findType, moduleDefinition);

                DependencyAttributeType = moduleDefinition.ImportReference(findType("AutoDI.DependencyAttribute"));
            }

            public class AutoDIExceptionsImport
            {
                public AutoDIExceptionsImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    TypeDefinition alreadyInitialized = findType("AutoDI.AlreadyInitializedException");
                    AlreadyInitializedException_Ctor = moduleDefinition.ImportReference(alreadyInitialized.GetConstructors().Single(x => !x.HasParameters));

                    var autoDIExceptionType = findType("AutoDI.AutoDIException");
                    AutoDIException_Ctor = moduleDefinition.ImportReference(autoDIExceptionType.GetConstructors().Single(c =>
                        c.Parameters.Count == 2 && c.Parameters[0].ParameterType.IsType<string>() &&
                        c.Parameters[1].ParameterType.IsType<Exception>()));
                }

                public MethodReference AlreadyInitializedException_Ctor { get; }

                public MethodReference AutoDIException_Ctor { get; }
            }

            public class IApplicationBuilderImport
            {
                public const string TypeName = "AutoDI.IApplicationBuilder";

                public IApplicationBuilderImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    Type = moduleDefinition.ImportReference(findType(TypeName));

                    TypeDefinition resolved = Type.Resolve();
                    ConfigureServices = moduleDefinition.ImportReference(resolved
                        .GetMethods()
                        .Single(x => x.Name == "ConfigureServices"));
                    Build = moduleDefinition.ImportReference(resolved.GetMethods().Single(x => x.Name == "Build"));
                }

                public TypeReference Type { get; }

                public MethodReference ConfigureServices { get; }

                public MethodReference Build { get; }
            }

            public class ApplicationBuilderImport
            {
                public ApplicationBuilderImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    Type = findType("AutoDI.ApplicationBuilder");

                    Ctor = moduleDefinition.ImportReference(Type.GetConstructors().Single(x => !x.HasParameters));
                }

                public TypeDefinition Type { get; }

                public MethodReference Ctor { get; }
            }

            public class GlobalDIImport
            {
                public GlobalDIImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    TypeDefinition globalDiType = findType("AutoDI.GlobalDI");


                    Register = moduleDefinition.ImportReference(globalDiType.GetMethods()
                        .Single(m => m.Name == "Register"));
                    Unregister = moduleDefinition.ImportReference(globalDiType.GetMethods()
                        .Single(m => m.Name == "Unregister"));
                    GetService = moduleDefinition.ImportReference(globalDiType.GetMethods()
                        .Single(m =>
                            m.Name == "GetService" && m.HasGenericParameters && m.Parameters.Count == 1));
                }

                public MethodReference Register { get; }
                public MethodReference Unregister { get; }
                public MethodReference GetService { get; }
            }

            public class ServiceCollectionMixinsImport
            {
                public MethodReference AddAutoDIService { get; }

                public ServiceCollectionMixinsImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
                {
                    var type = findType("AutoDI.ServiceCollectionMixins");

                    AddAutoDIService = moduleDefinition.ImportReference(
                        type.GetMethods().Single(m => m.Name == "AddAutoDIService"));
                }
            }
        }

        public class DependencyInjectionImport
        {
            public DependencyInjectionImport(Func<string, TypeDefinition> findType, ModuleDefinition moduleDefinition)
            {
                IServiceCollection = moduleDefinition.ImportReference(findType("Microsoft.Extensions.DependencyInjection.IServiceCollection"));

                var serviceProviderExtensions = findType("Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions");
                ServiceProviderServiceExtensions_GetService = moduleDefinition.ImportReference(serviceProviderExtensions.Methods.Single(x => x.Name == "GetService"));
            }

            public TypeReference IServiceCollection { get; }

            public MethodReference ServiceProviderServiceExtensions_GetService { get; }

        }



        public TypeReference IServiceProvider { get; }

        public TypeReference System_Exception { get; }
        public MethodReference System_AggregateException_Ctor { get; }
        public MethodReference System_Func2_Ctor { get; }

        public TypeDefinition List_Type { get; }
    }
}