﻿using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWtihNestedType
    {
        public class NestedType
        {
            public IService Service { get; }

            public NestedType([Dependency]IService service = null)
            {
                Service = service ?? throw new ArgumentNullException(nameof(service));
            }
        }
    }
}