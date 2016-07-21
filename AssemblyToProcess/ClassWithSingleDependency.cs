

using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithSingleDependency
    {
        public IService Service { get; }

        public ClassWithSingleDependency( [Dependency] IService service = null )
        {
            if ( service == null ) throw new ArgumentNullException( nameof( service ) );
            Service = service;
        }
    }
}