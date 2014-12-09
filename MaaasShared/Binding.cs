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
        static Logger logger = Logger.GetLogger("BindingHelper");

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
                        bindingObject = new JObject()
                        {
                            { defaultBindingAttribute, bindingObject }
                        };
                    }
                }
                else
                {
                    // Top level binding spec was not an object (was an array or value), so promote that value to be the value
                    // of the default binding attribute
                    //
                    bindingObject = new JObject()
                    {
                        { defaultBindingAttribute, bindingSpec.DeepClone() }
                    };
                }

                // Now that we've handled the default binding attribute cases, let's look for commands that need promotion...
                //
                if (commandAttributes != null)
                {
                    List<string> commandKeys = new List<string>();
                    foreach (var attribute in bindingObject)
                    {
                        if (commandAttributes.Contains(attribute.Key))
                        {
                            commandKeys.Add(attribute.Key);
                        }
                    }

                    foreach (var commandAttribute in commandAttributes)
                    {
                        // Processing a command (attribute name corresponds to a command)
                        //
                        if (bindingObject[commandAttribute] is JValue)
                        {
                            // If attribute value is simple value type, promote "attributeValue" to { command: "attributeValue" }
                            //
                            bindingObject[commandAttribute] = new JObject(){ { "command", bindingObject[commandAttribute] } };
                        }
                    }
                }

                logger.Debug("Found binding object: {0}", bindingObject);
            }
            else
            {
                // No binding spec
                bindingObject = new JObject();
            }

            return bindingObject;
        }
    }

    // PropertyValue objects maintain a list of things that provide values to the expanded output.  Some of
    // things things are binding contexts that will be evalutated each time the underlying value changes (one-way
    // bindings), but some of them will be resolved based on the initial view model contents at the time of
    // creation (one-time bindings).  This object accomodates both states and provides a convenient way to determine
    // which type of binding it is, and to extract the resolved/expanded value without needing to know which type
    // of binding it is.
    //
    public class BoundAndPossiblyResolvedToken
    {
        BindingContext _bindingContext;
        JToken _resolvedValue;

        // OK - The way negation is handled here is pretty crude.  The idea is that in the future we will support
        // complex value converters, perhaps even functions which themselves have more than one token as parameters.
        // So a more generalized concept of a value converter (delegate) passed in here from the parser and used
        // to produce the resolved value would be better.
        //
        bool _negated;

        public BoundAndPossiblyResolvedToken(BindingContext bindingContext, bool oneTime, bool negated)
        {
            _bindingContext = bindingContext;
            _negated = negated;

            if (oneTime)
            {
                // Since we're potentially storing this over time and don't want any underlying view model changes
                // to impact this value, we need to clone it.
                //
                _resolvedValue = _bindingContext.GetValue().DeepClone();
                if (_negated)
                {
                    _resolvedValue = new JValue(!TokenConverter.ToBoolean(_resolvedValue));
                }
            }
        }

        public BindingContext BindingContext { get { return _bindingContext; } }

        public bool Resolved { get { return _resolvedValue != null; } }

        public JToken ResolvedValue
        {
            get
            {
                if (_resolvedValue != null)
                {
                    return _resolvedValue;
                }
                else
                {
                    JToken resolvedValue = _bindingContext.GetValue();
                    if (_negated)
                    {
                        resolvedValue = new JValue(!TokenConverter.ToBoolean(resolvedValue));
                    }
                    return resolvedValue;
                }
            }
        }
    }

    // Property values consist of a string containing one or more "tokens", where such tokens are surrounded by curly brackets.
    // If the token is preceded with ^, then it is a "one-time" binding, otherwise it is a "one-way" (continuously updated)
    // binding.  Tokens can be negated (meaning their value will be converted to a boolean, and that value inverted) when 
    // preceded with !.  If both one-time binding and negation are specified for a token, the one-time binding indicator must
    // appear first.
    // 
    // Tokens that will resolve to numeric values may be followed by a colon and subsequent format specifier, using the .NET
    // Framework 4.5 format specifiers for numeric values.
    //
    // For example: 
    //
    //    "The scaling factor is {^scalingFactor:P2}".  
    //
    // The token is a one-time binding that will resolve to a number and then be formatted as a percentage with two decimal places:
    //
    //    "The scaling factor is 140.00 %"
    //
    public class PropertyValue
    {
        static Logger logger = Logger.GetLogger("PropertyValue");

        private static Regex _braceContentsRE = new Regex(@"{([^}]*)}");

        private string _formatString;
        private List<BoundAndPossiblyResolvedToken> _boundTokens;

        // Construct and return the unresolved binding contexts (the one-way bindings, excluding the one-time bindings)
        //
        public List<BindingContext> BindingContexts 
        { 
            get
            {
                List<BindingContext> bindingContexts = new List<BindingContext>();
                foreach (var boundToken in _boundTokens)
                {
                    if (!boundToken.Resolved)
                    {
                        bindingContexts.Add(boundToken.BindingContext);
                    }
                }
                return bindingContexts;
            }
        }

        public PropertyValue(string tokenString, BindingContext bindingContext)
        {
            _boundTokens = new List<BoundAndPossiblyResolvedToken>();
            int tokenIndex = 0;
            _formatString = _braceContentsRE.Replace(tokenString, delegate(Match m)
            {
                logger.Debug("Found boundtoken: {0}", m.Groups[1]);

                // Parse out any format specifier...
                //
                string token = m.Groups[1].ToString();
                string format = null;
                if (token.Contains(':'))
                {
                    string[] result = token.Split(':');
                    token = result[0];
                    format = result[1];
                }

                // Parse out and record any one-time binding indicator
                //
                bool oneTimeBinding = false;
                if (token.StartsWith("^"))
                {
                    token = token.Substring(1);
                    oneTimeBinding = true;
                }

                // Parse out and record negation indicator
                //
                bool negated = false;
                if (token.StartsWith("!"))
                {
                    token = token.Substring(1);
                    negated = true;
                }

                BoundAndPossiblyResolvedToken boundToken = new BoundAndPossiblyResolvedToken(bindingContext.Select(token), oneTimeBinding, negated);
                _boundTokens.Add(boundToken);

                if (format != null)
                {
                    return "{" + tokenIndex++ + ":" + format + "}";
                }
                else
                {
                    return "{" + tokenIndex++ + "}";
                }
            });
        }

        public JToken Expand()
        {
            if (_formatString == "{0}")
            {
                // If there is a binding containing exactly a single token, then that token may resolve to
                // a value of any type (not just string), and we want to preserve that type, so we process
                // that special case here...
                //
                BoundAndPossiblyResolvedToken token = _boundTokens[0];
                return token.ResolvedValue;
            }
            else
            {
                // Otherwise we replace all tokens with their values.  We convert numbers to their appropriate
                // types so that any format specifiers can be applied.  Note that we could easily add date support
                // here for data/time/timespan formatting if needed.
                //
                object[] resolvedTokens = new object[_boundTokens.Count];
                for (var i = 0; i < _boundTokens.Count; i++)
                {
                    BoundAndPossiblyResolvedToken token = _boundTokens[i];
                    JToken value = token.ResolvedValue;
                    if (value == null)
                    {
                        resolvedTokens[i] = "";
                    }
                    else if (value.Type == JTokenType.Float)
                    {
                        resolvedTokens[i] = (double)value;
                    }
                    else if (value.Type == JTokenType.Integer)
                    {
                        resolvedTokens[i] = (int)value;
                    }
                    else
                    {
                        resolvedTokens[i] = TokenConverter.ToString(value);
                    }
                }

                return new JValue(String.Format(_formatString, resolvedTokens));
            }
        }

        public static bool ContainsBindingTokens(string value)
        {
            return value.Contains("{");
        }

        public static JToken Expand(string tokenString, BindingContext bindingContext)
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

    public delegate void SetViewValue(JToken value);
    public delegate JToken GetViewValue();

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
            if (_setViewValue != null)
            {
                _setViewValue(_bindingContext.GetValue());
            }
        }

        public BindingContext BindingContext { get { return _bindingContext; } }
    }
}
