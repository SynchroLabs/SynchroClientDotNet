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
        JToken _boundToken;

        public Binding(JToken boundToken)
        {
            _bindingPath = boundToken.Path;
            _boundToken = boundToken;
            Util.debug("Creating binding with path: " + _bindingPath + " and current value of: " + _boundToken);
        }

        public string BindingPath { get { return _bindingPath; } }

        public JToken GetValue()
        {
            return _boundToken;
        }

        // Return boolean indicating whether the bound token was changed (and rebinding needs to be triggered)
        //
        public Boolean SetValue(object value)
        {
            return ViewModel.UpdateTokenValue(ref _boundToken, value);
        }

        public void Rebind(JToken rootBindingContext)
        {
            _boundToken = rootBindingContext.SelectToken(_bindingPath);
        }
    }

    static class BindingHelper
    {
        static Regex _bindingTokensRE = new Regex(@"[$]([^$]*)[.]");

        public static Binding ResolveBinding(string bindingPath, JToken rootBindingContext, JToken bindingContext)
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
        static Regex _onlyBraceContentsRE = new Regex(@"^{([^}]*)}$");

        public static bool ContainsBindingTokens(string value)
        {
            return value.Contains("{");
        }

        public static List<Binding> GetBoundTokens(string value, JToken rootBindingContext, JToken bindingContext)
        {
            List<Binding> boundTokens = new List<Binding>();
            _braceContentsRE.Replace(value, delegate(Match m)
            {
                Util.debug("Found boundtoken: " + m.Groups[1]);
                string token = m.Groups[1].ToString();
                boundTokens.Add(BindingHelper.ResolveBinding(token, rootBindingContext, bindingContext));
                return null;
            });

            return boundTokens;
        }

        public static object ExpandBoundTokens(string value, JToken rootBindingContext, JToken bindingContext = null)
        {
            if (bindingContext == null)
            {
                bindingContext = rootBindingContext;
            }

            if (_onlyBraceContentsRE.IsMatch(value))
            {
                // If there is a binding containing exactly a single token, then that token may resolve to
                // a value of any type (not just string), and we want to preserve that type, so we process
                // that special case here...
                //
                string token = _onlyBraceContentsRE.Match(value).Groups[1].ToString();
                Util.debug("Found binding that contained exactly one token: " + token);
                Binding binding = BindingHelper.ResolveBinding(token, rootBindingContext, bindingContext);
                return binding.GetValue();
            }
            else
            {
                // Otherwise we replace all tokens with their string values...
                //
                return _braceContentsRE.Replace(value, delegate(Match m)
                {
                    string token = m.Groups[1].ToString();
                    Util.debug("Found token in composite binding: " + token);
                    Binding binding = BindingHelper.ResolveBinding(token, rootBindingContext, bindingContext);
                    return (string)binding.GetValue();
                });
            }
        }

        public static string ExpandBoundTokensAsString(string value, JToken rootBindingContext, JToken bindingContext = null)
        {
            return (string)ExpandBoundTokens(value, rootBindingContext, bindingContext);
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

    public delegate void SetViewValue(object value);
    public delegate object GetViewValue();

    // For two-way binding (typically of primary "value" property) - binding to a single value only
    //
    public class ValueBinding
    {
        ViewModel _viewModel;
        Binding _binding;
        GetViewValue _getViewValue;
        SetViewValue _setViewValue;

        public Boolean IsDirty { get; set; }

        public ValueBinding(ViewModel viewModel, JToken rootBindingContext, JToken bindingContext, string value, GetViewValue getViewValue, SetViewValue setViewValue)
        {
            _viewModel = viewModel;
            _binding = BindingHelper.ResolveBinding(value, rootBindingContext, bindingContext);
            _getViewValue = getViewValue;
            _setViewValue = setViewValue;
            IsDirty = false;
        }

        public void UpdateViewModelFromView()
        {
            _viewModel.UpdateViewModelFromView(_binding, _getViewValue);
        }

        public void UpdateViewFromViewModel()
        {
            _setViewValue(_binding.GetValue());
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
        SetViewValue _setViewValue;

        public PropertyBinding(JToken rootBindingContext, JToken bindingContext, string value, SetViewValue setViewValue)
        {
            _rootBindingContext = rootBindingContext;
            _bindingContext = bindingContext;
            _rawValue = value;
            _bindings = BindingHelper.GetBoundTokens(_rawValue, _rootBindingContext, _bindingContext);
            _setViewValue = setViewValue;
        }

        public void UpdateViewFromViewModel()
        {
            this._setViewValue(BindingHelper.ExpandBoundTokens(_rawValue, _rootBindingContext, _bindingContext));
        }

        public List<Binding> Bindings { get { return _bindings; } }
    }
}
