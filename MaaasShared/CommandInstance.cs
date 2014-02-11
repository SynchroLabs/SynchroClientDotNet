using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public class CommandName
    {
        private CommandName(string attribute) { Attribute = attribute; }

        public string Attribute { get; private set; }

        public override string ToString() { return Attribute; }

        public static implicit operator String(CommandName commandName) { return commandName.Attribute; }

        public static CommandName OnClick { get { return new CommandName("onClick"); } }
        public static CommandName OnItemClick { get { return new CommandName("onItemClick"); } }
        public static CommandName OnSelectionChange { get { return new CommandName("onSelectionChange"); } }
        public static CommandName OnToggle { get { return new CommandName("onToggle"); } }
    }

    // This class corresponds to an instance of a command in a view
    //
    public class CommandInstance
    {
        string _command;
        Dictionary<string, JToken> _parameters = new Dictionary<string, JToken>();

        public CommandInstance(string command)
        {
            _command = command;
        }

        public void SetParameter(string parameterName, JToken parameterValue)
        {
            _parameters[parameterName] = parameterValue;
        }

        public string Command { get { return _command; } }

        // If a parameter is not a string type, then that parameter is passed directly.  This allows for parameters to
        // be boolean, numeric, or even objects.  If a parameter is a string, it will be evaluated to see if it has
        // any property bindings, and if so, those bindings will be expanded.  This allows for parameters that vary
        // based on the current context, for example, and also allows for complex values (such as property bindings
        // that refer to a single value of a type other than string, such as an object).
        //
        public JObject GetResolvedParameters(BindingContext bindingContext)
        {
            return new JObject(
                from parameter in _parameters
                select new JProperty(
                    parameter.Key,
                    parameter.Value.Type == JTokenType.String ? PropertyValue.Expand((string)parameter.Value, bindingContext) : parameter.Value
                    )
                );
        }
    }
}
