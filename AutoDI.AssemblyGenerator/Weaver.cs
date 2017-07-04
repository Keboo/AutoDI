using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace AutoDI.AssemblyGenerator
{
    public sealed class Weaver : DynamicObject
    {
        private readonly object _weaverInstance;
        public string Name { get; }

        internal Weaver(string name, object weaverInstance)
        {
            _weaverInstance = weaverInstance ?? throw new ArgumentNullException(nameof(weaverInstance));
            Name = name;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var members = _weaverInstance.GetType().GetMember(binder.Name);
            var member = members.SingleOrDefault();
            switch (member)
            {
                case PropertyInfo prop:
                    prop.SetValue(_weaverInstance, value);
                    return true;
            }
            return base.TrySetMember(binder, value);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var members = _weaverInstance.GetType().GetMember(binder.Name);
            var member = members.SingleOrDefault();
            switch (member)
            {
                case PropertyInfo prop:
                    result = prop.GetValue(_weaverInstance);
                    return true;
            }
            return base.TryGetMember(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = _weaverInstance.GetType().InvokeMember(binder.Name,
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, _weaverInstance, args);
            return true;
        }
    }
}
