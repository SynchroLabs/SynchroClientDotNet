using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient.Controls
{
    class ControlWrapper
    {
        StateManager _stateManager;
        ViewModel _viewModel;
        BindingContext _bindingContext;

        Dictionary<string, CommandInstance> _commands = new Dictionary<string, CommandInstance>();
        Dictionary<string, ValueBinding> _valueBindings = new Dictionary<string, ValueBinding>();
        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();
        List<ControlWrapper> _childControls = new List<ControlWrapper>();

        public ControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
            _bindingContext = bindingContext;
        }

        public ControlWrapper(ControlWrapper parent, BindingContext bindingContext)
        {
            _stateManager = parent.StateManager;
            _viewModel = parent.ViewModel;
            _bindingContext = bindingContext;
        }

        protected StateManager StateManager { get { return _stateManager; } }
        protected ViewModel ViewModel { get { return _viewModel; } }

        public BindingContext BindingContext { get { return _bindingContext; } }
        public List<ControlWrapper> ChildControls { get { return _childControls; } }

        protected void SetCommand(string attribute, CommandInstance command)
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

        protected void SetValueBinding(string attribute, ValueBinding valueBinding)
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

        // Process a value binding on an element.  If a value is supplied, a value binding to that binding context will be created.
        //
        protected Boolean processElementBoundValue(string attributeName, string value, GetViewValue getValue, SetViewValue setValue)
        {
            if (value != null)
            {
                BindingContext valueBindingContext = this.BindingContext.Select(value);
                ValueBinding binding = ViewModel.CreateAndRegisterValueBinding(valueBindingContext, value, getValue, setValue);
                SetValueBinding(attributeName, binding);
                return true;
            }

            return false;
        }

        // Process an element property, which can contain a plain value, a property binding token string, or no value at all,
        // in which case any optionally supplied defaultValue will be used.  This call *may* result in a property binding to
        // the element property, or it may not.
        //
        // This is "public" because there are cases when a parent element needs to process properties on its children after creation.
        //
        public void processElementProperty(string value, SetViewValue setValue, string defaultValue = null)
        {
            if (value == null)
            {
                if (defaultValue != null)
                {
                    setValue(defaultValue);
                }
                return;
            }
            else if (PropertyValue.ContainsBindingTokens(value))
            {
                // If value contains a binding, create a Binding and add it to metadata
                PropertyBinding binding = ViewModel.CreateAndRegisterPropertyBinding(this.BindingContext, value, setValue);
                _propertyBindings.Add(binding);
            }
            else
            {
                // Otherwise, just set the property value
                setValue(value);
            }
        }

        // This helper is used by control update handlers.
        //
        protected void updateValueBindingForAttribute(string attributeName)
        {
            ValueBinding binding = GetValueBinding(attributeName);
            if (binding != null)
            {
                // Update the local ViewModel from the element/control
                binding.UpdateViewModelFromView();
            }
        }

        // Process and record any commands in a binding spec
        //
        protected void ProcessCommands(JObject bindingSpec, string[] commands)
        {
            foreach (string command in commands)
            {
                JObject commandSpec = bindingSpec[command] as JObject;
                if (commandSpec != null)
                {
                    // A command spec contains an attribute called "command".  All other attributes are considered parameters.
                    //
                    CommandInstance commandInstance = new CommandInstance((string)commandSpec["command"]);
                    foreach (var property in commandSpec)
                    {
                        if (property.Key != "command")
                        {
                            commandInstance.SetParameter(property.Key, property.Value);
                        }
                    }
                    SetCommand(command, commandInstance);
                }
            }
        }

        // When we remove a control, we need to unbind it and its descendants (by unregistering all bindings
        // from the view model).  This is important as often times a control is removed when the underlying
        // bound values go away, such as when an array element is removed, causing a cooresponding (bound) list
        // or list view item to be removed.
        //
        public void Unregister()
        {
            foreach (ValueBinding valueBinding in _valueBindings.Values)
            {
                _viewModel.UnregisterValueBinding(valueBinding);
            }

            foreach (PropertyBinding propertyBinding in _propertyBindings)
            {
                _viewModel.UnregisterPropertyBinding(propertyBinding);
            }

            foreach (ControlWrapper childControl in _childControls)
            {
                childControl.Unregister();
            }
        }

        // This will create controls from a list of control specifications.  It will apply any "foreach" and "with" bindings
        // as part of the process.  It will call the supplied callback to actually create the individual controls.
        //
        public void createControls(BindingContext bindingContext, JArray controlList, Action<BindingContext, JObject> onCreateControl)
        {
            foreach (JObject element in controlList)
            {
                BindingContext controlBindingContext = bindingContext;
                Boolean controlCreated = false;

                if ((element["binding"] != null) && (element["binding"].Type == JTokenType.Object))
                {
                    Util.debug("Found binding object");
                    JObject bindingSpec = (JObject)element["binding"];
                    if (bindingSpec["foreach"] != null)
                    {
                        // First we create a BindingContext for the "foreach" path (a context to the elements to be iterated)
                        string bindingPath = (string)bindingSpec["foreach"];
                        Util.debug("Found 'foreach' binding with path: " + bindingPath);
                        BindingContext forEachBindingContext = bindingContext.Select(bindingPath);

                        // Then we determine the bindingPath to use on each element
                        string withPath = "$data";
                        if (bindingSpec["with"] != null)
                        {
                            // It is possible to use "foreach" and "with" together - in which case "foreach" is applied first
                            // and "with" is applied to each element in the foreach array.  This allows for path navigation
                            // both up to, and then after, the context to be iterated.
                            //
                            withPath = (string)bindingSpec["with"];
                        }

                        // Then we get each element at the foreach binding, apply the element path, and create the controls
                        List<BindingContext> bindingContexts = forEachBindingContext.SelectEach(withPath);
                        foreach (var elementBindingContext in bindingContexts)
                        {
                            Util.debug("foreach - creating control with binding context: " + elementBindingContext.BindingPath);
                            onCreateControl(elementBindingContext, element);
                        }
                        controlCreated = true;
                    }
                    else if (bindingSpec["with"] != null)
                    {
                        string withBindingPath = (string)bindingSpec["with"];
                        Util.debug("Found 'with' binding with path: " + withBindingPath);
                        controlBindingContext = bindingContext.Select(withBindingPath);
                    }
                }

                if (!controlCreated)
                {
                    onCreateControl(controlBindingContext, element);
                }
            }
        }
    }
}
