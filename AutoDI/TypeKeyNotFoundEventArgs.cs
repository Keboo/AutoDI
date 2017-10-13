using System;
using System.Collections.Generic;
using System.Text;

namespace AutoDI
{
    public class TypeKeyNotFoundEventArgs : EventArgs
    {
        Type Key_;
        DateTime EventTime_;

        public Type Key { get => Key_; }
        public DateTime EventTime { get => EventTime_; }

        public TypeKeyNotFoundEventArgs(Type key)
        {
            Key_ = key;
            EventTime_ = DateTime.Now;
        }

        public override string ToString() =>
            $"{EventTime_.ToString()}: Type key \"{Key_.ToString()}\" not found.";
    }
}
