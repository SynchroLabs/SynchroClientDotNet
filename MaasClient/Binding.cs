using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient
{
    public delegate void SetValue(String value);
    public delegate String GetValue();

    // For two-way binding of primary "value" property (binding to a single value only)
    public class ValueBinding
    {
        public string BoundValue { get; set; }
        public GetValue GetValue { get; set; }
        public SetValue SetValue { get; set; }
    }

    // For one-way binding of any property (binding to a pattern string than can incorporate multiple bound values)
    public class PropertyBinding
    {
        public string Content { get; set; }
        public SetValue SetValue { get; set; }
    }
}
