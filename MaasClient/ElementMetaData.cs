using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient
{
    public class ElementMetaData
    {
        List<PropertyBinding> _propertyBindings;

        public ElementMetaData()
        {
            _propertyBindings = new List<PropertyBinding>();
        }

        public String Command { get; set; }

        public JToken BindingContext { get; set; }

        public ValueBinding ValueBinding { get; set; }

        public List<PropertyBinding> PropertyBindings
        {
            get { return _propertyBindings; }
        }
    }
}
