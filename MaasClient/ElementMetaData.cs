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
        Dictionary<string, CommandInstance> _commands = new Dictionary<string, CommandInstance>();

        Dictionary<string, ValueBinding> _valueBindings = new Dictionary<string, ValueBinding>();

        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();

        public ElementMetaData()
        {
        }

        public JToken BindingContext { get; set; }

        public void SetCommand(string attribute, CommandInstance command)
        {
            _commands[attribute] = command;
        }

        public CommandInstance GetCommand(string attribute)
        {
            if (_commands.ContainsKey(attribute))
            {
                return _commands[attribute];
            }
            return null;
        }

        public void SetValueBinding(string attribute, ValueBinding valueBinding)
        {
            _valueBindings[attribute] = valueBinding;
        }

        public ValueBinding GetValueBinding(string attribute)
        {
            if (_valueBindings.ContainsKey(attribute))
            {
                return _valueBindings[attribute];
            }
            return null;
        }

        public List<PropertyBinding> PropertyBindings
        {
            get { return _propertyBindings; }
        }
    }
}
