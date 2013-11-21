using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaasClient
{
    public class Binding
    {
        string _bindingPath; // By storing the binding path, this give us the opportunity to "rebind" if needed (if changes
                             // to object/array values in the object state cause originally bound tokens to become invalid).

        public Binding(JToken boundToken)
        {
            _bindingPath = boundToken.Path;
            BoundToken = boundToken;
            Util.debug("Creating binding with path: " + _bindingPath + " and current value of: " + boundToken);
        }

        public string BindingPath { get { return _bindingPath; } }
        public JToken BoundToken { get; set; }
    }

    static class BindingHelper
    {
        static Regex _bindingTokensRE = new Regex(@"[$]([^$]*)[.]");

        public static Binding ResolveBinding(JToken rootBindingContext, JToken bindingContext, string bindingPath)
        {
            // Process path elements:
            //  $root
            //  $parent (and $parents[n]?)
            //  $data
            //  $index (inside foreach)
            //  $parentContext
            //
            JToken bindingContextBase = bindingContext;
            string relativeBindingPath = _bindingTokensRE.Replace(bindingPath, delegate(Match m)
            {
                string pathElement = m.Groups[1].ToString();
                Util.debug("Found binding path element: " + pathElement);
                if (pathElement == "root")
                {
                    bindingContextBase = rootBindingContext;
                }
                else if (pathElement == "parent")
                {
                    bindingContextBase = bindingContextBase.Parent;
                }
                return ""; // Removing the path elements as they are processed
            });

            return new Binding(bindingContextBase.SelectToken(relativeBindingPath));
        }

        static Regex _braceContentsRE = new Regex(@"{([^}]*)}");

        public static bool ContainsBindingTokens(string value)
        {
            return value.Contains("{");
        }

        public static List<Binding> GetBoundTokens(JToken rootBindingContext, JToken bindingContext, string value)
        {
            List<Binding> boundTokens = new List<Binding>();
            _braceContentsRE.Replace(value, delegate(Match m)
            {
                Util.debug("Found boundtoken: " + m.Groups[1]);
                string token = m.Groups[1].ToString();
                boundTokens.Add(BindingHelper.ResolveBinding(rootBindingContext, bindingContext, token));
                return null;
            });

            return boundTokens;
        }

        public static string ExpandBoundTokens(JToken rootBindingContext, JToken bindingContext, string value)
        {
            return _braceContentsRE.Replace(value, delegate(Match m)
            {
                Util.debug("Found binding: " + m.Groups[1]);
                string token = m.Groups[1].ToString();
                Binding binding = BindingHelper.ResolveBinding(rootBindingContext, bindingContext, token);
                return (string)binding.BoundToken;
            });
        }

        // Binding is specified in the "binding" attribute of an element.  For example, binding: { value: "foo" } will bind the "value"
        // property of the control to the "foo" value in the current binding context.  For controls that can call commands, the command
        // handlers are bound similarly, for example, binding: { onClick: "someCommand" } will bind the onClick action of the control to
        // the "someCommand" command.
        //
        // A control type may have a default binding attribute, so that a simplified syntax may be used, where the binding contains a
        // simple value to be bound to the default binding attribute of the control.  For example, an edit control might use binding: "username"
        // to bind the default attribute ("value") to username.  A button might use binding: "someCommand" to bind the default attribute ("onClick")
        // to someCommand.
        //
        // This function extracts the binding value, and if the default/shorthand notation is used, expands it to a fully specified binding object.
        //
        //     For example, for an edit control with a default binding attribute of "value" a binding of:
        //
        //       binding: "username"
        //
        //           becomes
        //
        //       binding: {value: "username"}
        //
        public static JObject GetCanonicalBindingSpec(JObject controlSpec, string defaultBindingAttribute)
        {
            JObject bindingObject = null;

            if (controlSpec["binding"] != null)
            {
                if (controlSpec["binding"].Type == JTokenType.Object)
                {
                    // Value for binding was an object - return it
                    bindingObject = (JObject)controlSpec["binding"];
                }
                else if (controlSpec["binding"].Type == JTokenType.String)
                {
                    // Standalone string value in binding - compose binding object
                    string bindingString = "{ " + defaultBindingAttribute + ": \"" + (string)controlSpec["binding"] + "\" }";
                    bindingObject = JObject.Parse(bindingString);
                }
                else
                {
                    // Standalone non-string value in binding - compose binding object
                    string bindingString = "{ " + defaultBindingAttribute + ": " + controlSpec["binding"].ToString(Newtonsoft.Json.Formatting.None) + " }";
                    bindingObject = JObject.Parse(bindingString);
                }

                Util.debug("Found binding object: " + bindingObject);
            }
            else
            {
                bindingObject = new JObject();
            }
            return bindingObject;
        }

    }

    public delegate void SetValue(String value);
    public delegate String GetValue();

    // For two-way binding of primary "value" property (binding to a single value only)
    //
    public class ValueBinding
    {
        ViewModel _viewModel;
        Binding _binding;
        GetValue _getValue;
        SetValue _setValue;

        public ValueBinding(ViewModel viewModel, JToken rootBindingContext, JToken bindingContext, string value, GetValue getValue, SetValue setValue)
        {
            _viewModel = viewModel;
            _binding = BindingHelper.ResolveBinding(rootBindingContext, bindingContext, value);
            _getValue = getValue;
            _setValue = setValue;
        }

        public void UpdateValue()
        {
            _viewModel.UpdateValue(_binding, _getValue);
        }

        public void UpdateView()
        {
            _setValue((string)_binding.BoundToken);
        }

        public string GetViewValue()
        {
            // The binding value is updated from the view and the updated binding value is returned.
            ((JValue)_binding.BoundToken).Value = _getValue();
            return (string)_binding.BoundToken;
        }

        public Binding Binding { get { return _binding; } }
    }

    // For one-way binding of any property (binding to a pattern string than can incorporate multiple bound values)
    //
    public class PropertyBinding
    {
        JToken _rootBindingContext;
        JToken _bindingContext;
        string _rawValue;
        List<Binding> _bindings = new List<Binding>();
        SetValue _setValue;

        public PropertyBinding(JToken rootBindingContext, JToken bindingContext, string value, SetValue setValue)
        {
            _rootBindingContext = rootBindingContext;
            _bindingContext = bindingContext;
            _rawValue = value;
            _bindings = BindingHelper.GetBoundTokens(_rootBindingContext, _bindingContext, _rawValue);
            _setValue = setValue;
        }

        public void UpdateView()
        {
            this._setValue(BindingHelper.ExpandBoundTokens(_rootBindingContext, _bindingContext, _rawValue));
        }

        public List<Binding> Bindings { get { return _bindings; } }
    }
}
