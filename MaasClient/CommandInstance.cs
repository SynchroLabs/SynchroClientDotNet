using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient
{
    // This class corresponds to an instance of a command in a view
    //
    public class CommandInstance
    {
        string _command;
        Dictionary<string, string> _parameters = new Dictionary<string, string>();

        public CommandInstance(string command, Dictionary<string, string> parameters = null)
        {
            _command = command;
            if (parameters != null)
            {
                foreach (var parameterName in parameters.Keys)
                {
                    _parameters[parameterName] = parameters[parameterName];
                }
            }
        }

        public void SetParameter(string parameterName, string parameterValue)
        {
            _parameters[parameterName] = parameterValue;
        }

        public string Command { get { return _command; } }

        public JObject GetResolvedParameters(BindingContext bindingContext)
        {
            return new JObject(
                from parameter in _parameters
                select new JProperty(
                    parameter.Key,
                    PropertyValue.Expand(parameter.Value, bindingContext)
                    )
                );
        }
    }
}
