

using System;
using AutoDI;

namespace AssemblyToProcess
{
    public class ClassWithCustomResolver
    {
        public IService Service { get; }
        
        public ClassWithCustomResolver( [Dependency] IService service = null )
        {
            var request = new ResolverRequest(typeof(ClassWithCustomResolver), new[] {typeof(IService), typeof(IService2)});
            var dr = DependencyResolver.Get(request);
            if ( service == null ) throw new ArgumentNullException( nameof( service ) );
            Service = service;
        }
    }
}