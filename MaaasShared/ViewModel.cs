using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    // The ViewModel will manage the client view model data (initialize and update it).  It will also manage all bindings
    // to view model data, including managing updates on changes.
    //
    public sealed class ViewModel
    {
        static Logger logger = Logger.GetLogger("ViewModel");

        BindingContext _rootBindingContext;
        JObject _rootObject;
        Boolean _updatingView = false;

        List<ValueBinding> _valueBindings = new List<ValueBinding>();
        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();

        public ViewModel()
        {
            _rootBindingContext = new BindingContext(this);
        }

        public BindingContext RootBindingContext { get { return _rootBindingContext; } }

        public JObject RootObject { get { return _rootObject; } } // Only used by BindingContext - "internal"?

        public ValueBinding CreateAndRegisterValueBinding(BindingContext bindingContext, GetViewValue getValue, SetViewValue setValue)
        {
            ValueBinding valueBinding = new ValueBinding(this, bindingContext, getValue, setValue);
            _valueBindings.Add(valueBinding);
            return valueBinding;
        }

        public void UnregisterValueBinding(ValueBinding valueBinding)
        {
            _valueBindings.Remove(valueBinding);
        }

        public PropertyBinding CreateAndRegisterPropertyBinding(BindingContext bindingContext, string value, SetViewValue setValue)
        {
            PropertyBinding propertyBinding = new PropertyBinding(bindingContext, value, setValue);
            _propertyBindings.Add(propertyBinding);
            return propertyBinding;
        }

        public void UnregisterPropertyBinding(PropertyBinding propertyBinding)
        {
            _propertyBindings.Remove(propertyBinding);
        }

        // Tokens in the view model have a "ViewModel." prefix (as the view model itself is a child node of a larger
        // JSON response).  We need to prune that off so future SelectToken operations will work when applied to the
        // root binding context (the context associated with the "ViewModel" JSON object).
        //
        public static string GetTokenPath(JToken token)
        {
            string path = token.Path;

            if (path.StartsWith("ViewModel."))
            {
                path = path.Remove(0, "ViewModel.".Length);
            }

            return path;
        }

        public void InitializeViewModelData(JObject viewModel)
        {
            if (viewModel == null)
            {
                viewModel = new JObject();
            }
            _rootObject = viewModel;
            _valueBindings.Clear();
            _propertyBindings.Clear();
            _rootBindingContext.Rebind();
        }

        // If a complex assignment takes place, then the token itself will be replaced.  In that case, this method
        // will return true and will update the passed in token.
        //
        public static Boolean UpdateTokenValue(ref JToken token, object value)
        {
            if (!(value is JToken))
            {
                // This will convert standard value types (string, bool, int, double, etc) to JValue for assignment
                //
                value = new JValue(value);
            }

            // If the bound item value and the value being set are both primitive values, then we just do a value assignment,
            // otherwise we have to do a token replace (which may modify the object graph and trigger rebinding).
            //
            if ((token is JValue) && (value is JValue))
            {
                ((JValue)token).Value = ((JValue)value).Value;
            }
            else
            {
                JToken newValue = (JToken)value;
                token.Replace(newValue);
                if (newValue.Parent != null)
                {
                    // Replace has a shortcut that doesn't actually do the replace if the new value is equal to the 
                    // current value.  The only way we can really know if the replace happened is to see if the new
                    // value has its parent set (it is null before the replace, and on an actual replace, is set to
                    // the parent of the replaced token).
                    token = newValue;
                    return true; // Rebinding is required (token change)
                }
            }
            return false; // Rebinding is not required (value-only change, or no change)
        }

        // This object represents a binding update (the path of the bound item and an indication of whether rebinding is required)
        //
        public class BindingUpdate
        {
            public string BindingPath { get; set; }
            public bool RebindRequired { get; set; }

            public BindingUpdate(string bindingPath, bool rebindRequired)
            {
                BindingPath = bindingPath;
                RebindRequired = rebindRequired;
            }
        }

        // If bindingUpdates is provided, any binding other than an optionally specified sourceBinding
        // that is impacted by a token in bindingUpdates will have its view updated.  If no bindingUpdates
        // is provided, all bindings will have their view updated.  
        //
        // If bindingUpdates is provided, any binding impacted by a path for which rebinding is indicated
        // will be rebound.
        //
        // Usages:
        //    On new view model - no params - update view for all bindings, no rebind needed
        //    On update view model - pass list containing all updates 
        //    On update view (from ux) - pass list containing the single update, and the sourceBinding (that triggered the update)
        //
        public void UpdateViewFromViewModel(List<BindingUpdate> bindingUpdates = null, BindingContext sourceBinding = null)
        {
            _updatingView = true;

            foreach (ValueBinding valueBinding in _valueBindings)
            {
                if (valueBinding.BindingContext != sourceBinding)
                {
                    bool isUpdateRequired = (bindingUpdates == null);
                    bool isBindingDirty = false;
                    if (bindingUpdates != null)
                    {
                        foreach (BindingUpdate update in bindingUpdates)
                        {
                            if (valueBinding.BindingContext.IsBindingUpdated(update.BindingPath, update.RebindRequired))
                            {
                                isUpdateRequired = true;
                                if (update.RebindRequired)
                                {
                                    isBindingDirty = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (isBindingDirty)
                    {
                        logger.Debug("Rebind value binding with path: {0}", valueBinding.BindingContext.BindingPath);
                        valueBinding.BindingContext.Rebind();
                    }

                    if (isUpdateRequired)
                    {
                        valueBinding.UpdateViewFromViewModel();
                    }
                }
            }

            foreach (PropertyBinding propertyBinding in _propertyBindings)
            {
                var isUpdateRequired = (bindingUpdates == null);

                foreach (BindingContext propBinding in propertyBinding.BindingContexts)
                {
                    bool isBindingDirty = false;
                    if (bindingUpdates != null)
                    {
                        foreach (BindingUpdate update in bindingUpdates)
                        {
                            if (propBinding.IsBindingUpdated(update.BindingPath, update.RebindRequired))
                            {
                                isUpdateRequired = true;
                                if (update.RebindRequired)
                                {
                                    isBindingDirty = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (isBindingDirty)
                    {
                        logger.Debug("Rebind property binding with path: {0}", propBinding.BindingPath);
                        propBinding.Rebind();
                    }
                }

                if (isUpdateRequired)
                {
                    propertyBinding.UpdateViewFromViewModel();
                }
            }

            _updatingView = false;
        }

        public void UpdateViewModelData(JToken viewModelDeltas, Boolean updateView = true)
        {
            List<BindingUpdate> bindingUpdates = new List<BindingUpdate>();

            logger.Debug("Processing view model updates: {0}", viewModelDeltas);
            if ((viewModelDeltas.Type == JTokenType.Array))
            {
                // Removals are generally reported as removals from the end of the list with increasing indexes.  If
                // we process them in this way, the first removal will change the list positions of remaining items
                // and cause subsequent removals to be off (typically to fail).  And we don't really want to rely
                // on ordering in the first place.  So what we are going to do is track all of the removals, and then
                // actually remove them at the end.
                //
                List<JToken> removals = new List<JToken>();

                foreach (JObject viewModelDelta in (JArray)viewModelDeltas)
                {
                    string path = (string)viewModelDelta["path"];
                    string changeType = (string)viewModelDelta["change"];

                    logger.Debug("View model item change ({0}) for path: {1}", changeType, path);
                    if (changeType == "object")
                    {
                        // For "object" changes, this just means that an existing object had a property added/updated/removed or
                        // an array had items added/updated/removed.  We don't need to actually do any updates for this notification,
                        // we just need to make sure any bound elements get their views updated appropriately.
                        //
                        bindingUpdates.Add(new BindingUpdate(path, false));
                    }
                    else if (changeType == "update")
                    {
                        JToken vmItemValue = _rootObject.SelectToken(path);
                        if (vmItemValue != null)
                        {
                            logger.Debug("Updating view model item for path: {0} to value: {1}", path, viewModelDelta["value"]);

                            bool rebindRequired = UpdateTokenValue(ref vmItemValue, viewModelDelta["value"]);
                            bindingUpdates.Add(new BindingUpdate(path, rebindRequired));
                        }
                        else
                        {
                            logger.Error("VIEW MODEL SYNC WARNING: Unable to find existing value when processing update, something went wrong, path: {0}", path);
                        }
                    }
                    else if (changeType == "add")
                    {
                        logger.Debug("Adding bound item for path: {0} with value: {1}", path, viewModelDelta["value"]);
                        bindingUpdates.Add(new BindingUpdate(path, true));

                        // First, double check to make sure the path doesn't actually exist
                        JToken vmItemValue = _rootObject.SelectToken(path, false);
                        if (vmItemValue == null)
                        {
                            if (path.EndsWith("]"))
                            {
                                // This is an array element...
                                string parentPath = path.Substring(0, path.LastIndexOf("["));
                                JToken parentToken = _rootObject.SelectToken(parentPath);
                                if ((parentToken != null) && (parentToken is JArray))
                                {
                                    ((JArray)parentToken).Add(viewModelDelta["value"]);
                                }
                                else
                                {
                                    logger.Error("VIEW MODEL SYNC WARNING: Attempt to add array member, but parent didn't exist or was not an array, parent path: {0}", parentPath);
                                }
                            }
                            else if (path.Contains("."))
                            {
                                // This is an object property...
                                string parentPath = path.Substring(0, path.LastIndexOf("."));
                                string attributeName = path.Substring(path.LastIndexOf(".") + 1);
                                JToken parentToken = _rootObject.SelectToken(parentPath);
                                if ((parentToken != null) && (parentToken is JObject))
                                {
                                    ((JObject)parentToken).Add(attributeName, viewModelDelta["value"]);
                                }
                                else
                                {
                                    logger.Error("VIEW MODEL SYNC WARNING: Attempt to add object property, but parent didn't exist or was not an object, parent path: {0}", parentPath);
                                }
                            }
                            else
                            {
                                // This is a root property...
                                _rootObject.Add(path, viewModelDelta["value"]);
                            }
                        }
                        else
                        {
                            logger.Error("VIEW MODEL SYNC WARNING: Found existing value when processing add, something went wrong, path: {0}", path);
                        }
                    }
                    else if (changeType == "remove")
                    {
                        logger.Debug("Removing bound item for path: {0}", path);
                        bindingUpdates.Add(new BindingUpdate(path, true));

                        JToken vmItemValue = _rootObject.SelectToken(path);
                        if (vmItemValue != null)
                        {
                            logger.Debug("Removing bound item for path: {0}", vmItemValue.Path);
                            // Just track this removal for now - we'll remove it at the end
                            removals.Add(vmItemValue);
                        }
                        else
                        {
                            logger.Error("VIEW MODEL SYNC WARNING: Attempt to remove object property or array element, but it wasn't found, path: {0}", path);
                        }
                    }
                }

                // Remove all tokens indicated as removed
                foreach (JToken vmItemValue in removals)
                {
                    vmItemValue.Remove();
                }

                logger.Debug("View model after processing updates: {0}", this._rootObject);
            }

            if (updateView)
            {
                UpdateViewFromViewModel(bindingUpdates);
            }
        }

        // This is called when a value change is triggered from the UX, specifically when the control calls
        // the UpdateValue member of it's ValueBinding.  We will change the value, record the change, and
        // update any binding that depends on this value.  This is the mechanism that allows for "client side
        // dynamic binding".
        //
        public void UpdateViewModelFromView(BindingContext bindingContext, GetViewValue getValue)
        { 
            if (_updatingView)
            {
                // When we update the view from the view model, the UX generates a variety of events to indicate
                // that values changed (text changed, list contents changed, selection changed, etc).  We don't 
                // want those events to trigger a view model update (and mark as dirty), so we bail here.  This 
                // check is not sufficient (by itself), since some of these events can be posted and will show up
                // asynchronously, so we do some other checks, but this is quick and easy and catches most of it.
                //
                return;
            }

            var newValue = getValue();
            var currentValue = bindingContext.GetValue();
            if (newValue == currentValue)
            {
                // Only record changes and update dependant UX objects for actual value changes - some programmatic 
                // changes to set the view to the view model state will trigger otherwise unidentifiable change events,
                // and this check will weed those out (if they got by the _updatingView check above).
                //
                return;
            }

            // Update the view model
            //
            var rebindRequired = bindingContext.SetValue(newValue);

            // Find the ValueBinding that triggered this update and mark it as dirty...
            //
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                if (valueBinding.BindingContext == bindingContext)
                {
                    // logger.Debug("Marking dirty - binding with path: {0}", bindingContext.BindingPath);
                    valueBinding.IsDirty = true;
                }
            }

            // Process all of the rest of the bindings (rebind and update view as needed)...
            //
            List<BindingUpdate> bindingUpdates = new List<BindingUpdate>();
            bindingUpdates.Add(new BindingUpdate(bindingContext.BindingPath, rebindRequired));
            UpdateViewFromViewModel(bindingUpdates, bindingContext);
        }

        public bool IsDirty()
        {
            foreach (ValueBinding valueBinding in _valueBindings)
            {
                if (valueBinding.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<string, JToken> CollectChangedValues()
        {
            var vmDeltas = new Dictionary<string, JToken>();

            foreach (ValueBinding valueBinding in _valueBindings)
            {
                if (valueBinding.IsDirty)
                {
                    string path = valueBinding.BindingContext.BindingPath;
                    JToken value = valueBinding.BindingContext.GetValue();
                    logger.Debug("Changed view model item - path: {0} - value: {1}", path, value);
                    vmDeltas[path] = value;
                    valueBinding.IsDirty = false;
                }
            }

            return vmDeltas;
        }
    }
}
