﻿using System;

namespace AutoDI.Container.Fody
{
    [Flags]
    public enum Behaviors
    {
        None = 0,
        SingleInterfaceImplementation = 1,
        IncludeClasses = 2,
        Default = SingleInterfaceImplementation | IncludeClasses
    }
}