using System;

namespace AutoDI
{
    public class ResolverRequest
    {
        public Type CallerType { get; }

        public Type[] Dependencies { get; }

        public ResolverRequest( Type callerType, Type[] dependencies )
        {
            CallerType = callerType;
            Dependencies = dependencies;
        }
    }
}