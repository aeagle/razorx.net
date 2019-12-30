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

        private readonly ExpandoObject props = new ExpandoObject();

        public RazorXProps Add<T>(string name, T val)
        {
            ((IDictionary<string, object>)props).Add(name, val);

            return this;
        }

        public dynamic Build()
        {
            return props;
        }
    }
}
