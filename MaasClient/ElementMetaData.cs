using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MaasClient
{
    public class ElementMetaData
    {
        Dictionary<string, CommandInstance> _commands = new Dictionary<string, CommandInstance>();

        Dictionary<string, ValueBinding> _valueBindings = new Dictionary<string, ValueBinding>();

        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();

        List<FrameworkElement> _childElements = new List<FrameworkElement>();

        public ElementMetaData()
        {
        }

        public BindingContext BindingContext { get; set; }

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

        // This is a read-only list (effectively), so callers changing it will be disappointed
        public List<ValueBinding> ValueBindings { get { return new List<ValueBinding>(_valueBindings.Values); } }

        public List<PropertyBinding> PropertyBindings { get { return _propertyBindings; } }

        public List<FrameworkElement> ChildElements { get { return _childElements; } }
    }
}
