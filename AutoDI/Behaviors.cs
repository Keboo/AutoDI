using System;

namespace AutoDI
{
    [Flags]
    public enum Behaviors
    {
        None = 0,
        SingleInterfaceImplementation = 1,
        IncludeClasses = 2,
        IncludeBaseClasses = 4,
        IncludeDependentAutoDIAssemblies = 8,
        Default = SingleInterfaceImplementation | IncludeClasses | IncludeBaseClasses | IncludeDependentAutoDIAssemblies
    }
}