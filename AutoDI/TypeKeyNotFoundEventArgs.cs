using System;

namespace AutoDI
{
    public class TypeKeyNotFoundEventArgs : EventArgs
    {
        public Type ServiceKey { get; }

        public object Instance { get; set; }

        public TypeKeyNotFoundEventArgs(Type serviceKey)
        {
            ServiceKey = serviceKey;
        }
    }
}
