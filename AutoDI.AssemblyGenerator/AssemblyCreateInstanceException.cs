using System;

namespace AutoDI.AssemblyGenerator
{
    public class AssemblyCreateInstanceException : Exception
    {
        public AssemblyCreateInstanceException(string message) 
            : base(message)
        {
            
        }
    }
}