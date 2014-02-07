using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class BindingContextListItem
    {
        BindingContext _bindingContext;
        string _item;

        public BindingContextListItem(BindingContext bindingContext, string item)
        {
            _bindingContext = bindingContext;
            _item = item;
        }

        public BindingContext BindingContext { get { return _bindingContext; } }

        public override string ToString()
        {
            return _bindingContext.Select(_item).GetValue().ToString();
        }

        public JToken GetValue()
        {
            return _bindingContext.Select("$data").GetValue();
        }

        public JToken GetSelection(string selectionItem)
        {
            return _bindingContext.Select(selectionItem).GetValue().DeepClone();
        }

        public override bool Equals(System.Object obj)
        {
            BindingContextListItem item = obj as BindingContextListItem;
            if (item == null)
            {
                return false;
            }

            return _bindingContext.BindingPath.Equals(item._bindingContext.BindingPath);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    class WinPhonePickerWrapper : WinPhoneControlWrapper
    {
        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        public WinPhonePickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");
            ListPicker picker = new ListPicker();
            this._control = picker;

            applyFrameworkElementDefaults(picker);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", new string[] { "onItemClick" });
            if (bindingSpec != null)
            {
                ProcessCommands(bindingSpec, new string[] { "onItemClick" });

                if (bindingSpec["items"] != null)
                {
                    string itemSelector = (string)bindingSpec["item"];
                    if (itemSelector == null)
                    {
                        itemSelector = "$data";
                    }

                    processElementBoundValue(
                        "items",
                        (string)bindingSpec["items"],
                        () => getPickerContents(picker),
                        value => this.setPickerContents(picker, GetValueBinding("items").BindingContext, itemSelector));
                }

                if (bindingSpec["selection"] != null)
                {
                    string selectionItem = (string)bindingSpec["selectionItem"];
                    if (selectionItem == null)
                    {
                        selectionItem = "$data";
                    }

                    processElementBoundValue(
                        "selection",
                        (string)bindingSpec["selection"],
                        () => getPickerSelection(picker, selectionItem),
                        value => this.setPickerSelection(picker, selectionItem, (JToken)value));
                }
            }

            picker.SelectionChanged += picker_SelectionChanged;
        }

        public JToken getPickerContents(ListPicker picker)
        {
            Util.debug("Getting picker contents");
            return new JArray(
                from BindingContextListItem item in picker.Items
                select item.GetValue()
                );
        }

        public void setPickerContents(ListPicker picker, BindingContext bindingContext, string itemSelector)
        {
            Util.debug("Setting picker contents");

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");

            _selectionChangingProgramatically = true;

            picker.Items.Clear();

            foreach (BindingContext itemContext in itemContexts)
            {
                BindingContextListItem pickerItem = new BindingContextListItem(itemContext, itemSelector);
                picker.Items.Add(pickerItem);
            }

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                // If there is a "selection" value binding, then we update the selection state from that after filling the list.
                //
                selectionBinding.UpdateViewFromViewModel();
            }
            else if (_localSelection != null)
            {
                // If there is not a "selection" value binding, then we use local selection state to restore the selection when
                // re-filling the list.
                //
                this.setPickerSelection(picker, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getPickerSelection(ListPicker picker, string selectionItem)
        {
            BindingContextListItem item = (BindingContextListItem)picker.SelectedItem;
            if (item != null)
            {
                return item.GetSelection(selectionItem);
            }
            return new JValue(false); // This is a "null" selection
        }

        public void setPickerSelection(ListPicker picker, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            bool itemSelected = false;
            foreach (BindingContextListItem item in picker.Items)
            {
                if (JToken.DeepEquals(selection, item.GetSelection(selectionItem)))
                {
                    picker.SelectedItem = item;
                    itemSelected = true;
                    break;
                }
            }

            if (!itemSelected)
            {
                // No way to clear the selection (if setting via index or reference, must be valid value).  If we get
                // here it means the selection being set does not match any available values.  Bad.
                //
                picker.SelectedIndex = 0;
            }

            _selectionChangingProgramatically = false;
        }

        void picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Util.debug("Picker selection changed");
            ListPicker picker = (ListPicker)sender;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getPickerSelection(picker, "$data");
            }

            if (!_selectionChangingProgramatically)
            {
                CommandInstance command = GetCommand("onItemClick");
                if (command != null)
                {
                    Util.debug("Picker item click with command: " + command);

                    // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                    //
                    if ((e.AddedItems != null) && (e.AddedItems.Count > 0))
                    {
                        BindingContextListItem listItem = (BindingContextListItem)e.AddedItems[0];
                        StateManager.processCommand(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                    }
                }
            }
        }
    }
}
