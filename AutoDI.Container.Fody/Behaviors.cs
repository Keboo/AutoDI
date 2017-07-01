using System;

namespace AutoDI.Container.Fody
{
    [Flags]
    internal enum Behaviors
    {
        None = 0,
        SingleInterfaceImplementation = 1,
        IncludeClasses = 2,
        IncludeDerivedClasses = 4,
        IncludeDependentAutoDIAssemblies = 8,
        Default = SingleInterfaceImplementation | IncludeClasses | IncludeDerivedClasses | IncludeDependentAutoDIAssemblies
    }
}