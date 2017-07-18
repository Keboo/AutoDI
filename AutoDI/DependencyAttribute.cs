using System;

namespace AutoDI
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class DependencyAttribute : Attribute
    {
        public DependencyAttribute(params object[] parameters)
        { }
    }
}