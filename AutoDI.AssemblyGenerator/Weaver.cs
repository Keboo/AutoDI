using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;

namespace AutoDI.AssemblyGenerator
{
    public sealed class Weaver : DynamicObject
    {
        private const string WeaverTypeName = "ModuleWeaver";

        public static Weaver FindWeaver(string weaverName)
        {
            object ProcessAssembly(Assembly assembly)
            {
                Type weaverType = assembly.GetType(WeaverTypeName);
                if (weaverType == null) return null;
                return Activator.CreateInstance(weaverType);
            }

            string assemblyName = $"{weaverName}.Fody";

            try
            {
                object weaverInstance = ProcessAssembly(Assembly.Load(assemblyName));
                if (weaverInstance != null)
                    return new Weaver(weaverName, weaverInstance);
            }
            catch (Exception ex)
            {
                // ignored
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName == assemblyName))
            {
                object weaverInstance = ProcessAssembly(assembly);
                if (weaverInstance != null)
                    return new Weaver(weaverName, weaverInstance);
            }

            throw new Exception($"Failed to find weaver '{weaverName}'. Could not locate {weaverName}.Fody assembly.");
        }

        private readonly object _weaverInstance;
        public string Name { get; }

        internal Weaver(string name, object weaverInstance)
        {
            _weaverInstance = weaverInstance ?? throw new ArgumentNullException(nameof(weaverInstance));
            Name = name;
        }

        public void ApplyToAssembly(string assemblyPath)
        {
            using (var fileStream = File.Open(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
            {
                ApplyToAssembly(fileStream);
            }
        }

        public void ApplyToAssembly(Stream assemblyStream)
        {
            dynamic dynamicWeaver = this;
            var module = ModuleDefinition.ReadModule(assemblyStream);
            dynamicWeaver.ModuleDefinition = module;
            var errors = new List<string>();
            dynamicWeaver.LogError = new Action<string>(s =>
            {
                Debug.WriteLine($" Error: {s}");
                errors.Add(s);
            });
            dynamicWeaver.LogWarning = new Action<string>(s => Debug.WriteLine($" Warning: {s}"));
            dynamicWeaver.LogInfo = new Action<string>(s => Debug.WriteLine($" Info: {s}"));
            dynamicWeaver.LogDebug = new Action<string>(s => Debug.WriteLine($" Debug: {s}"));
            dynamicWeaver.AssemblyResolver = new DefaultAssemblyResolver();
            dynamicWeaver.Execute();
            if (errors.Any())
            {
                throw new WeaverErrorException(errors);
            }
            assemblyStream.Position = 0;
            module.Write(assemblyStream);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var members = _weaverInstance.GetType().GetMember(binder.Name);
            var member = members.SingleOrDefault();
            switch (member)
            {
                case PropertyInfo prop:
                    prop.SetValue(_weaverInstance, value);
                    return true;
            }
            return base.TrySetMember(binder, value);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var members = _weaverInstance.GetType().GetMember(binder.Name);
            var member = members.SingleOrDefault();
            switch (member)
            {
                case PropertyInfo prop:
                    result = prop.GetValue(_weaverInstance);
                    return true;
            }
            return base.TryGetMember(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = _weaverInstance.GetType().InvokeMember(binder.Name,
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, _weaverInstance, args);
            return true;
        }
    }
}
