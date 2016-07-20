using System;

namespace AutoDI
{
    [AttributeUsage( AttributeTargets.Parameter )]
    public class DependencyAttribute : Attribute
    {
        
    }
}