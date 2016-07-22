using System;

namespace AutoDI
{
    [AttributeUsage( AttributeTargets.Parameter )]
    public sealed class DependencyAttribute : Attribute
    {
        public DependencyAttribute(params object[] parameters)
        { }
    }
}