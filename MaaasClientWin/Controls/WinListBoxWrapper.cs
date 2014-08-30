using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinListBoxWrapper : WinControlWrapper
    {
        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        bool _selectionModeNone = false;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public WinListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listbox element");
            ListBox listbox = new ListBox();
            this._control = listbox;

            applyFrameworkElementDefaults(listbox);

            ListSelectionMode mode = ToListSelectionMode(controlSpec["select"]);
            switch (mode)
            {
                case ListSelectionMode.None:
                    // The Windows ListBox doesn't support a "None" selection mode, so we use Single and track the "None" ourselves.
                    _selectionModeNone = true;
                    listbox.SelectionMode = SelectionMode.Single;
                    break;
                case ListSelectionMode.Single:
                    listbox.SelectionMode = SelectionMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    listbox.SelectionMode = SelectionMode.Multiple;
                    break;
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListboxContents(listbox),
                    value => this.setListboxContents(listbox, GetValueBinding("items").BindingContext, itemContent));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListboxSelection(listbox, selectionItem),
                    value => this.setListboxSelection(listbox, selectionItem, (JToken)value));
            }

            listbox.SelectionChanged += listbox_SelectionChanged;
        }

        public JToken getListboxContents(ListBox listbox)
        {
            Util.debug("Getting listbox contents");
            return new JArray(
                from BindingContextListItem item in listbox.Items
                select item.GetValue()
                );
        }

        public void setListboxContents(ListBox listbox, BindingContext bindingContext, string itemContent)
        {
            Util.debug("Setting listbox contents");

            _selectionChangingProgramatically = true;

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");

            listbox.Items.Clear();
            foreach (BindingContext itemContext in itemContexts)
            {
                BindingContextListItem listItem = new BindingContextListItem(itemContext, itemContent);
                listbox.Items.Add(listItem);
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
                this.setListboxSelection(listbox, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getListboxSelection(ListBox listbox, string selectionItem)
        {
            if (listbox.SelectionMode == SelectionMode.Multiple)
            {
                return new JArray(
                    from BindingContextListItem item in listbox.SelectedItems
                    select item.GetSelection(selectionItem)
                    );
            }
            else
            {
                BindingContextListItem item = (BindingContextListItem)listbox.SelectedItem;
                if (item != null)
                {
                    return item.GetSelection(selectionItem);
                }
                return new JValue(false); // This is a "null" selection
            }
        }

        public void setListboxSelection(ListBox listbox, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;
            if ((listbox.SelectionMode == SelectionMode.Multiple) && (selection is JArray))
            {
                listbox.SelectedItems.Clear();
                foreach (BindingContextListItem listItem in listbox.Items)
                {
                    JArray array = selection as JArray;
                    foreach (JToken item in array.Values())
                    {
                        if (JToken.DeepEquals(item, listItem.GetSelection(selectionItem)))
                        {
                            listbox.SelectedItems.Add(listItem);
                            break;
                        }
                    }
                }
            }
            else
            {
                bool itemSelected = false;
                foreach (BindingContextListItem listItem in listbox.Items)
                {
                    if (JToken.DeepEquals(selection, listItem.GetSelection(selectionItem)))
                    {
                        listbox.SelectedItem = listItem;
                        itemSelected = true;
                        break;
                    }
                }

                if (!itemSelected)
                {
                    listbox.SelectedItem = null;
                }
            }
            _selectionChangingProgramatically = false;
        }

        void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            Util.debug("Listbox selection changed");
            ListBox listbox = (ListBox)sender;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getListboxSelection(listbox, "$data");
            }

            if (!_selectionChangingProgramatically)
            {
                if (_selectionModeNone)
                {
                    listbox.SelectedItem = null;
                    CommandInstance command = GetCommand(CommandName.OnItemClick);
                    if (command != null)
                    {
                        // For selection mode "None", the command handler resolves its tokens relative to the item selected.
                        //
                        // There should always be a first "added" item, which represents the current selection (item clicked).
                        //
                        if ((e.AddedItems != null) && (e.AddedItems.Count > 0))
                        {
                            BindingContextListItem listItem = (BindingContextListItem)e.AddedItems[0];
                            StateManager.processCommand(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                        }
                    }
                }
                else
                {
                    Util.debug("Selection changed by user!");
                    CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                    if (command != null)
                    {
                        Util.debug("ListView item click with command: " + command);

                        if (listbox.SelectionMode == SelectionMode.Single)
                        {
                            // For selection mode "Single", the command handler resolves its tokens relative to the item selected.
                            //
                            // There should always be a first "added" item, which represents the current selection.
                            //
                            if ((e.AddedItems != null) && (e.AddedItems.Count > 0))
                            {
                                BindingContextListItem listItem = (BindingContextListItem)e.AddedItems[0];
                                StateManager.processCommand(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                            }
                        }
                        else if (listbox.SelectionMode == SelectionMode.Multiple)
                        {
                            // For selection mode "Multiple", the command hander resovles its tokens relative to the listbox, not any list item(s).
                            //
                            StateManager.processCommand(command.Command, command.GetResolvedParameters(this.BindingContext));
                        }
                    }
                }
            }
        }
    }
}
