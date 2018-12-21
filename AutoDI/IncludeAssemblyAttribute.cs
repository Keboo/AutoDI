using System;

namespace AutoDI
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IncludeAssemblyAttribute : Attribute
    {
        public string AssemblyPattern { get; }

        public IncludeAssemblyAttribute(string assemblyPattern)
        {
            AssemblyPattern = assemblyPattern;
        }
    }
}