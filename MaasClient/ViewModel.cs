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
                    // !!! Probably want to handle object changes in second pass (so all primitive values and added array
                    //     elements are processed before we start updating (and triggering UX updates) for objects.
                    //
                    // !!! May want to track paths of changes (so we can figure out who relied on changed items after the
                    //     update and notify them, potentially after rebinding).
                    //
                    string path = (string)bindingChange["path"];
                    string changeType = (string)bindingChange["change"];

                    Util.debug("Bound item change (" + changeType + ") for path: " + path);
                    if (changeType == "update")
                    {
                        JToken boundValue = _boundItems.SelectToken(path);
                        if (boundValue != null)
                        {
                            Util.debug("Updating bound item for path: " + path + " to value: " + bindingChange["value"]);
                            // !!! Intitially, we tried:
                            //
                            //     boundValue.Replace(bindingChange["value"]);
                            //
                            //     That was fairly flexible (in terms of accepting any kind of JToken as a value), but it broke the
                            //     binding by replaceing the JToken.  So now we do the below, which only works as long as we're only
                            //     doing value updates (changes of value types on existing tokens).  So this is kind of fragile and
                            //     needs some work.  The old way should work (including setting complex values) after rebinding is 
                            //     implemented.
                            //
                            //     Right now, if you have a value foo: none and it get's changed to foo: ["one", "two"], this will
                            //     fail (it will generate an "update" change where the new value is a JArray, which is not (and thus
                            //     cannot be cast to) a JValue below.
                            //
                            ((JValue)boundValue).Value = ((JValue)bindingChange["value"]).Value;
                        }
                    }
                    else if (changeType == "add")
                    {
                        Util.debug("Adding bound item for path: " + path + " with value: " + bindingChange["value"]);

                        // First, double check to make sure the path doesn't actually exist
                        JToken boundValue = _boundItems.SelectToken(path, false);
                        if (boundValue != null)
                        {
                            // !!! Should we do something about this?  Just set the value?
                            Util.debug("WARNING: Found existing value when processing add, something went wrong, path: " + path);
                        }

                        if (path.EndsWith("]"))
                        {
                            // This is an array element...
                            string parentPath = path.Substring(0, path.LastIndexOf("["));
                            JToken parentToken = _boundItems.SelectToken(parentPath);
                            if ((parentToken != null) && (parentToken is JArray))
                            {
                                ((JArray)parentToken).Add(bindingChange["value"]);
                            }
                            else
                            {
                                // !!! Do something?
                                Util.debug("ERROR: Attempt to add array member, but parent didn't exist or was not an array, parent path: " + parentPath);
                            }
                        }
                        else if (path.Contains("."))
                        {
                            // This is an object property...
                            string parentPath = path.Substring(0, path.LastIndexOf("."));
                            string attributeName = path.Substring(path.LastIndexOf(".") + 1);
                            JToken parentToken = _boundItems.SelectToken(parentPath);
                            if ((parentToken != null) && (parentToken is JObject))
                            {
                                ((JObject)parentToken).Add(attributeName, bindingChange["value"]);
                            }
                            else
                            {
                                // !!! Do something?
                                Util.debug("ERROR: Attempt to add object property, but parent didn't exist or was not a property, parent path: " + parentPath);
                            }
                        }
                        else
                        {
                            // This is a root property...
                            _boundItems.Add(path, bindingChange["value"]);
                        }

                    }
                    else if (changeType == "remove")
                    {
                        JToken boundValue = _boundItems.SelectToken(path);
                        if (boundValue != null)
                        {
                            Util.debug("Removing bound item for path: " + boundValue.Path);
                            boundValue.Remove();
                        }
                        else
                        {
                            // !!! Do something?
                            Util.debug("ERROR: Attempt to remove object property or array element, but it wasn't found, path: " + path);
                        }
                    }
                }

                // !!! It is worth noting that at this point (after updates) bindings may not be correct.  Tokens that are bound may no
                //     longer be valid (because they were deleted or replaced) and paths that failed binding previously may now succeed
                //     if re-attempted (as new items may have shown up that match those paths).  So this is where it might be good to
                //     "re-bind" all bindings (use their full path to see if they would still bind to the same token, and if not, bind
                //     them to the new token). 
                //
                Util.debug("Bound values after processing updates: " + this._boundItems);
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
            ((JValue)binding.BoundToken).Value = value; // !!! Fail on non-primitive value (like JArray from multi-select listbox)

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

        public void CollectChangedValues(Action<string, JToken> setValue)
        {
            // !!! Right now this just gets all bound values that are capable of changing (should just get the values that actually changed)
            //
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                // Remove base context path element ("BoundItems.") from beginning of path...
                string path = valueBinding.Binding.BoundToken.Path.Remove(0, _boundItems.Path.Length + 1);
                JToken value = valueBinding.GetViewValue(); // !!! any
                Util.debug("Bound item path: " + path + " - value: " + value);
                setValue(path, value);
            }
        }
    }
}
