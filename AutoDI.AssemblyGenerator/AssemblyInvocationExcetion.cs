using System;

namespace AutoDI. AssemblyGenerator
{
    public class AssemblyInvocationExcetion : Exception
    {
        public AssemblyInvocationExcetion(string message) : base(message)
        { }
    }
}