using System;

namespace AutoDI.Mono.Cecil
{
    internal class AutoDIBuildException : Exception
    {
        public AutoDIBuildException()
        {
            
        }

        public AutoDIBuildException(string message) : base(message)
        {
            
        }
    }
}