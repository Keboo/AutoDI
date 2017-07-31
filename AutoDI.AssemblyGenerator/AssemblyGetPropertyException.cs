using System;

namespace AutoDI.AssemblyGenerator
{
    public class AssemblyGetPropertyException : Exception
    {
        public AssemblyGetPropertyException(string message) : base(message)
        { }
    }
}