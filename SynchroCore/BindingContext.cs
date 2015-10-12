using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SynchroCore
{
    // Corresponds to a specific location in a JSON oject (which may or may not exist at the time the BindingContext is created).
    //
    public class BindingContext
    {
        static Logger logger = Logger.GetLogger("BindingContext");

        JObject _bindingRoot;

        string _bindingPath;
        JToken _boundToken;
        bool _isIndex = false;

        public JObject BindingRoot
        {
            get { return _bindingRoot; }
            set
            {
                if (value != _bindingRoot)
                {
                    _bindingRoot = value;
                    this.Rebind();
                }
            }
        }

        // Creates the root binding context, from which all other binding contexts will be created
        //
        public BindingContext(JObject bindingRoot)
        {
            _bindingRoot = bindingRoot;
            _bindingPath = "";
            _boundToken = _bindingRoot;
        }

        private void attemptToBindTokenIfNeeded()
        {
            if (_boundToken == null)
            {
                _boundToken = _bindingRoot.SelectToken(_bindingPath);
            }
        }

        private static Regex _bindingTokensRE = new Regex(@"[$]([^.]*)[.]?"); // Token starts with $, separated by dot

        private void resolveBinding(string parentPath, string bindingPath)
        {
            // Process path elements:
            //
            //  $root
            //  $parent
            //  $data
            //  $index
            //
            _bindingPath = _bindingTokensRE.Replace(bindingPath, delegate(Match m)
            {
                string pathElement = m.Groups[1].ToString();
                logger.Debug("Found binding path element: {0}", pathElement);
                if (pathElement == "root")
                {
                    parentPath = "";
                }
                else if (pathElement == "parent")
                {
                    if (parentPath.Length != 0)
                    {
                        var lastDot = parentPath.LastIndexOf(".");
                        if (lastDot == -1)
                        {
                            // Remove the only remaining path segment
                            parentPath = "";
                        }
                        else
                        {
                            // Remove the last (rightmost) path segment
                            parentPath = parentPath.Remove(lastDot);
                        }
                    }
                }
                else if (pathElement == "data")
                {
                    // We're going to treat $data as a noop
                }
                else if (pathElement == "index")
                {
                    _isIndex = true;
                }

                return ""; // Removing the path elements as they are processed
            });

            if ((parentPath.Length > 0) && (_bindingPath.Length > 0))
            {
                _bindingPath = parentPath + "." + _bindingPath;
            }
            else if (parentPath.Length > 0)
            {
                _bindingPath = parentPath;
            }
        }

        private BindingContext(BindingContext context, string bindingPath)
        {
            _bindingRoot = context._bindingRoot;
            resolveBinding(context._bindingPath, bindingPath);
            this.attemptToBindTokenIfNeeded();
        }

        private BindingContext(BindingContext context, int index, string bindingPath)
        {
            _bindingRoot = context._bindingRoot;
            resolveBinding(context._bindingPath + "[" + index + "]", bindingPath);
            this.attemptToBindTokenIfNeeded();
        }

        //
        // Public interface starts here...
        //

        // Given a path to a changed element, determine if the binding is impacted.
        //
        public Boolean IsBindingUpdated(string updatedElementPath, Boolean objectChange)
        {
            if (objectChange && (_bindingPath.StartsWith(updatedElementPath)))
            {
                // If this is an object change (meaning the object/array itself changed), then a binding
                // update is required if the path matches or is an ancestor of the binging path.
                //
                return true;
            }
            else if (_bindingPath.Equals(updatedElementPath))
            {
                // If this is a primitive value change, or an object/array contents change (meaning
                // that the object itself did not change), then a binding update is only required if
                // the path matches exactly.
                //
                return true;
            }

            return false;
        }

        public BindingContext Select(string bindingPath)
        {
            return new BindingContext(this, bindingPath);
        }

        public List<BindingContext> SelectEach(string bindingPath)
        {
            List<BindingContext> bindingContexts = new List<BindingContext>();

            if ((_boundToken != null) && (_boundToken.Type == JTokenType.Array))
            {
                int index = 0;
                foreach (JToken arrayElement in (JArray)_boundToken)
                {
                    bindingContexts.Add(new BindingContext(this, index, bindingPath));
                    index++;
                }
            }

            return bindingContexts;
        }

        public string BindingPath { get { return _bindingPath; } }

        public JToken GetValue()
        {
            this.attemptToBindTokenIfNeeded();
            if (_boundToken != null)
            {
                if (_isIndex)
                {
                    // Find first ancestor that is an array and get the position of that ancestor's child
                    //
                    JToken child = _boundToken;
                    JToken parent = child.Parent;

                    while (parent != null)
                    {
                        JArray parentArray = parent as JArray;
                        if (parentArray != null)
                        {
                            return new JValue(parentArray.IndexOf(child));
                        }
                        else
                        {
                            child = parent;
                            parent = child.Parent;
                        }
                    }
                }
                else
                {
                    return _boundToken;
                }
            }

            // Token could not be bound at this time (no corresponding token) - no value returned!
            return null;
        }

        // Return boolean indicating whether the bound token was changed (and rebinding needs to be triggered)
        //
        public Boolean SetValue(JToken value)
        {
            this.attemptToBindTokenIfNeeded();
            if (_boundToken != null)
            {
                if (_isIndex)
                {
                    return false;
                }
                else
                {
                    return JToken.UpdateTokenValue(ref _boundToken, value);
                }
            }

            // Token could not be bound at this time (no corresponding token) - value not set!
            //
            // !!! This happens when you bind a control to a ViewModel path that is not initialized to a value.  In
            //     this case, any changes to that value will not be recorded (there is no place to record the value
            //     in the local ViewModel).  Maybe it would be better to try to create the ViewModel item locally, or
            //     to have a mechanism for recording the value so it could still be sent (and result in an "Add" coming
            //     back from the server after diff processing).
            //
            logger.Info("Unable to store new value {0} for element: {1}, element not initialized by app", value, this._bindingPath);
            return false;
        }

        public void Rebind()
        {
            _boundToken = _bindingRoot.SelectToken(_bindingPath);
        }
    }
}
