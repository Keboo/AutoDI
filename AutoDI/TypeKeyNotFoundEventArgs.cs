using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDI
{
    public class TypeKeyNotFoundEventArgs : EventArgs
    {
        private readonly Type _Key;
        private readonly DateTime _EventTime;

        public Type Key { get => _Key; }
        public DateTime EventTime { get => _EventTime; }

        public TypeKeyNotFoundEventArgs(Type key)
        {
            _Key = key;
            _EventTime = DateTime.Now;
        }

        public override string ToString() =>
            $"{_EventTime.ToString()}: Type key \"{_Key.ToString()}\" not found.";
    }
}
