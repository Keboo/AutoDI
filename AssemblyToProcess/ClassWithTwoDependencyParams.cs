using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithTwoDependencyParams
    {
        public IService Service { get; }

        public ClassWithTwoDependencyParams( [Dependency(4, "Test")]IService service = null )
        {
            if ( service == null ) throw new ArgumentNullException( nameof( service ) );
            Service = service;
        }
    }
}