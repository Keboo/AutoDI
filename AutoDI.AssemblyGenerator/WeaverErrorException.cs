using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoDI.AssemblyGenerator
{
    public class WeaverErrorException : Exception
    {
        public string[] Errors { get; }

        public WeaverErrorException(ICollection<string> errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            Errors = errors.ToArray();
        }
    }
}