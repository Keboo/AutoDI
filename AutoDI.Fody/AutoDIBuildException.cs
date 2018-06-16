using System;

namespace AutoDI.Fody
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