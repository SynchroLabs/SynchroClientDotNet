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
        Dictionary<string, ValueBinding> _valueBindings = new Dictionary<string, ValueBinding>();
        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();

        public ElementMetaData()
        {
        }

        public String Command { get; set; }

        public JToken BindingContext { get; set; }

        public void SetValueBinding(string attribute, ValueBinding valueBinding)
        {
            _valueBindings[attribute] = valueBinding;
        }

        public ValueBinding GetValueBinding(string attribute)
        {
            return _valueBindings[attribute];
        }

        public List<PropertyBinding> PropertyBindings
        {
            get { return _propertyBindings; }
        }
    }
}
