using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaaasCore
{
    public static class BindingHelper
    {
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
        //         becomes
        //
        //       binding: {value: "username"}
        //
        //     For commands:
        //
        //       binding: "doSomething"
        //
        //         becomes
        //
        //       binding: { onClick: "doSomething" }
        //
        //         becomes
        //
        //       binding: { onClick: { command: "doSomething" } }
        //
        //     Also (default binding atttribute is 'onClick', which is also in command attributes list):
        //
        //       binding: { command: "doSomething" value: "theValue" }
        //
        //         becomes
        //
        //       binding: { onClick: { command: "doSomething", value: "theValue" } }
        //
        public static JObject GetCanonicalBindingSpec(JObject controlSpec, string defaultBindingAttribute, string[] commandAttributes = null)
        {
            JObject bindingObject = null;

            bool defaultAttributeIsCommand = false;
            if (commandAttributes != null)
            {
                defaultAttributeIsCommand = commandAttributes.Contains(defaultBindingAttribute);
            }

            JToken bindingSpec = controlSpec["binding"];

            if (bindingSpec != null)
            {
                if (bindingSpec.Type == JTokenType.Object)
                {
                    // Encountered an object spec, return that (subject to further processing below)
                    //
                    bindingObject = (JObject)bindingSpec.DeepClone();

                    if (defaultAttributeIsCommand && (bindingObject["command"] != null))
                    {
                        // Top-level binding spec object contains "command", and the default binding attribute is a command, so
                        // promote { command: "doSomething" } to { defaultBindingAttribute: { command: "doSomething" } }
                        //
                        bindingObject = new JObject(
                            new JProperty(defaultBindingAttribute, bindingObject)
                            );
                    }
                }
                else
                {
                    // Top level binding spec was not an object (was an array or value), so promote that value to be the value
                    // of the default binding attribute
                    //
                    bindingObject = new JObject(
                        new JProperty(defaultBindingAttribute, bindingSpec.DeepClone())
                        );
                }

                // Now that we've handled the default binding attribute cases, let's look for commands that need promotion...
                //
                if (commandAttributes != null)
                {
                    foreach (var attribute in bindingObject)
                    {
                        if (commandAttributes.Contains(attribute.Key))
                        {
                            // Processing a command (attribute name corresponds to a command)
                            //
                            if (attribute.Value is JValue)
                            {
                                // If attribute value is simple value type, promote "attributeValue" to { command: "attributeValue" }
                                //
                                attribute.Value.Replace(new JObject(new JProperty("command", attribute.Value)));
                            }
                        }
                    }
                }

                Util.debug("Found binding object: " + bindingObject);
            }
            else
            {
                // No binding spec
                bindingObject = new JObject();
            }

            return bindingObject;
        }
    }

    public class PropertyValue
    {
        private static Regex _braceContentsRE = new Regex(@"{([^}]*)}");

        private string _formatString;
        private List<BindingContext> _boundTokens;

        public List<BindingContext> BindingContexts { get { return _boundTokens; } }

        public PropertyValue(string tokenString, BindingContext bindingContext)
        {
            _boundTokens = new List<BindingContext>();
            int tokenIndex = 0;
            _formatString = _braceContentsRE.Replace(tokenString, delegate(Match m)
            {
                Util.debug("Found boundtoken: " + m.Groups[1]);
                string token = m.Groups[1].ToString();
                if (token.StartsWith("^"))
                {
                    // One time binding, resolve now...
                    token = token.Substring(1);
                    return bindingContext.Select(token).GetValue().ToString();
                }
                else
                {
                    // Normal token binding, record binding context and return format spec token...
                    _boundTokens.Add(bindingContext.Select(token));
                    return "{" + tokenIndex++ + "}";
                }
            });
        }

        public object Expand()
        {
            if (_formatString == "{0}")
            {
                // If there is a binding containing exactly a single token, then that token may resolve to
                // a value of any type (not just string), and we want to preserve that type, so we process
                // that special case here...
                //
                BindingContext tokenContext = _boundTokens[0];
                return tokenContext.GetValue();
            }
            else
            {
                // Otherwise we replace all tokens with their string values...
                //
                string[] expandedTokens = new string[_boundTokens.Count];
                for (var i = 0; i < _boundTokens.Count; i++)
                {
                    BindingContext tokenContext = _boundTokens[i];
                    expandedTokens[i] = (string)tokenContext.GetValue();
                }

                return String.Format(_formatString, expandedTokens);
            }
        }

        public static bool ContainsBindingTokens(string value)
        {
            return value.Contains("{");
        }

        public static object Expand(string tokenString, BindingContext bindingContext)
        {
            PropertyValue propertyValue = new PropertyValue(tokenString, bindingContext);
            return propertyValue.Expand();
        }

        public static string ExpandAsString(string tokenString, BindingContext bindingContext)
        {
            return Expand(tokenString, bindingContext).ToString();
        }
    }

    //
    // Actual bindings: Property (one-way, composite) and Value (two-way, single value)
    //

    public delegate void SetViewValue(object value);
    public delegate object GetViewValue();

    // For one-way binding of any property (binding to a pattern string than can incorporate multiple bound values)
    //
    public class PropertyBinding
    {
        PropertyValue _propertyValue;
        SetViewValue _setViewValue;

        public PropertyBinding(BindingContext bindingContext, string value, SetViewValue setViewValue)
        {
            _propertyValue = new PropertyValue(value, bindingContext);
            _setViewValue = setViewValue;
        }

        public void UpdateViewFromViewModel()
        {
            this._setViewValue(_propertyValue.Expand());
        }

        public List<BindingContext> BindingContexts { get { return _propertyValue.BindingContexts; } }
    }

    // For two-way binding (typically of primary "value" property) - binding to a single value only
    //
    public class ValueBinding
    {
        ViewModel _viewModel;
        BindingContext _bindingContext;
        GetViewValue _getViewValue;
        SetViewValue _setViewValue;

        public Boolean IsDirty { get; set; }

        public ValueBinding(ViewModel viewModel, BindingContext bindingContext, GetViewValue getViewValue, SetViewValue setViewValue)
        {
            _viewModel = viewModel;
            _bindingContext = bindingContext;
            _getViewValue = getViewValue;
            _setViewValue = setViewValue;
            IsDirty = false;
        }

        public void UpdateViewModelFromView()
        {
            _viewModel.UpdateViewModelFromView(_bindingContext, _getViewValue);
        }

        public void UpdateViewFromViewModel()
        {
            _setViewValue(_bindingContext.GetValue());
        }

        public BindingContext BindingContext { get { return _bindingContext; } }
    }
}
