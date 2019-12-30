using System.Collections.Generic;
using System.Dynamic;

namespace RazorX.ViewEngine
{
    public class RazorXProps
    {
        public static RazorXProps Create()
        {
            return new RazorXProps();
        }

        private readonly NullingExpandoObject props = new NullingExpandoObject();

        public RazorXProps Add<T>(string name, T val)
        {
            props.Add(name, val);
            return this;
        }

        public dynamic Build()
        {
            return props;
        }
    }

    public class NullingExpandoObject : DynamicObject
    {
        private readonly Dictionary<string, object> values
            = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // We don't care about the return value...
            values.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            values[binder.Name] = value;
            return true;
        }

        public void Add(string name, object value)
        {
            values[name] = value;
        }
    }
}
