using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaasClient
{
    // The ViewModel will manage the client view model data (initialize and update it).  It will also manage all bindings
    // to view model data, including managing updates on changes.
    //
    // !!! Initial bound items tree could be called ViewModel and updates could be called ViewModelUpdates
    //
    // !!! On client side, keep track of dirty flag.  Serialize all dirty items.  Clear dirty flag.
    //
    public sealed class ViewModel
    {
        JObject _boundItems;

        List<ValueBinding> _valueBindings = new List<ValueBinding>();
        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();

        public ViewModel()
        {
        }

        public JObject BoundItems { get { return _boundItems; } }

        public ValueBinding CreateValueBinding(JToken bindingContext, string value, GetValue getValue, SetValue setValue)
        {
            ValueBinding valueBinding = new ValueBinding(this, _boundItems, bindingContext, value, getValue, setValue);
            _valueBindings.Add(valueBinding);
            return valueBinding;
        }

        public PropertyBinding CreatePropertyBinding(JToken bindingContext, string value, SetValue setValue)
        {
            PropertyBinding propertyBinding = new PropertyBinding(_boundItems, bindingContext, value, setValue);
            _propertyBindings.Add(propertyBinding);
            return propertyBinding;
        }

        public void InitializeViewModelData(JObject boundItems)
        {
            _boundItems = boundItems;
        }

        public void UpdateViewModelData(JToken boundItems)
        {
            Util.debug("Processing bound item updates: " + boundItems);
            if ((boundItems.Type == JTokenType.Array))
            {
                foreach (JObject bindingChange in (JArray)boundItems)
                {
                    // !!! This only works for update really.  Need to think through add/remove cases.  Probably
                    //     want to handle object changes in second pass (so all primitive values and added array elements
                    //     are processed before we start updating (and triggering UX updates) for objects.
                    //
                    // !!! For add - it should be the case that the parent item (object or array) exists, so we can back
                    //     up one and set it.  This is different if it's an object versus array (pretty sure).
                    //
                    // !!! For remove - similarly, the parent should exist, so we can back up and set it (object vs array
                    //     differences still in play)
                    //
                    string changeType = (string)bindingChange["change"];
                    if (changeType == "update")
                    {
                        Util.debug("Found bound item change for path: " + bindingChange["path"]);
                        JToken boundValue = _boundItems.SelectToken((string)bindingChange["path"]);
                        if (boundValue != null)
                        {
                            Util.debug("Updating bound item change for path: " + boundValue.Path + " to value: " + bindingChange["value"]);
                            boundValue.Replace(bindingChange["value"]);
                            //Util.debug("Bound values after change update: " + this.boundItems);
                        }
                    }
                }
            }
        }

        // This is called when a value change is triggered from the UX, specifically when the control calls
        // the UpdateValue member of it's ValueBinding.  We will change the value, record the change, and
        // update any binding that depends on this value.  This is the mechanism that allows for "client side
        // dynamic binding".
        //
        public void UpdateValue(Binding binding, GetValue getValue)
        { 
            // !!! Only update/record/notify if value is actually different?

            // !!! Record the change (mark as dirty)

            // Update the value
            var value = getValue();
            Util.debug("Got value update: " + value);
            ((JValue)binding.BoundToken).Value = value;

            // Notify ValueBinding dependencies (except for the ValueBinding that triggered this)
            //
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                if (valueBinding.Binding != binding)
                {
                    if (valueBinding.Binding.BindingPath == binding.BindingPath)
                    {
                        valueBinding.UpdateView();
                    }
                }
            }

            // Notify PropertyBinding dependencies
            //
            foreach (PropertyBinding propertyBinding in _propertyBindings)
            {
                bool isDependency = false;

                foreach (Binding propBinding in propertyBinding.Bindings)
                {
                    if (propBinding.BindingPath == binding.BindingPath)
                    {
                        isDependency = true;
                    }
                }

                if (isDependency)
                {
                    propertyBinding.UpdateView();
                }
            }
        }

        public void UpdateView()
        {
            // !!! Right now this updates all bindings.  Ideally, should be tracking changes and updating
            //     only bindings that depend on changed values (except for initial view update).

            // Update ValueBindings
            //
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                valueBinding.UpdateView();
            }

            // Update PropertyBindings
            //
            foreach (PropertyBinding propertyBinding in _propertyBindings)
            {
                propertyBinding.UpdateView();
            }
        }

        public void CollectChangedValues(Action<string, string> setValue)
        {
            // !!! Right now this just gets all bound values that are capable of changing
            //
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                // Remove base context path element ("BoundItems.") from beginning of path...
                string path = valueBinding.Binding.BoundToken.Path.Remove(0, _boundItems.Path.Length + 1);
                String value = valueBinding.GetViewValue();
                Util.debug("Bound item path: " + path + " - value: " + value);
                setValue(path, value);
            }
        }
    }
}
