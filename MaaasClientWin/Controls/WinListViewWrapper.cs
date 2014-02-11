﻿using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinListViewWrapper : WinControlWrapper
    {
        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public WinListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listview element");
            ListView listView = new ListView();
            this._control = listView;

            applyFrameworkElementDefaults(listView);

            ListSelectionMode mode = ToListSelectionMode(controlSpec["select"]);
            switch (mode)
            {
                case ListSelectionMode.None:
                    listView.SelectionMode = ListViewSelectionMode.None;
                    break;
                case ListSelectionMode.Single:
                    listView.SelectionMode = ListViewSelectionMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    listView.SelectionMode = ListViewSelectionMode.Multiple;
                    break;
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListViewContents(listView),
                    value => this.setListViewContents(listView, (JObject)controlSpec["itemTemplate"], GetValueBinding("items").BindingContext));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListViewSelection(listView, selectionItem),
                    value => this.setListViewSelection(listView, selectionItem, (JToken)value));
            }

            if (listView.SelectionMode != ListViewSelectionMode.None)
            {
                listView.SelectionChanged += listView_SelectionChanged;
            }
            else
            {
                listView.IsItemClickEnabled = true;
                listView.ItemClick += listView_ItemClick;
            }
        }

        public JToken getListViewContents(ListView listbox)
        {
            Util.debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(ListView listview, JObject itemTemplate, BindingContext bindingContext)
        {
            Util.debug("Setting listview contents");

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");

            if (listview.Items.Count < itemContexts.Count)
            {
                // New items are added (to the end of the list)
                //
                for (int index = listview.Items.Count; index < itemContexts.Count; index++)
                {
                    WinControlWrapper controlWrapper = CreateControl(this, itemContexts[index], itemTemplate);
                    listview.Items.Add(controlWrapper.Control);
                }
            }
            else if (listview.Items.Count > itemContexts.Count)
            {
                // Items need to be removed (from the end of the list)
                //
                for (int index = listview.Items.Count; index > itemContexts.Count; index--)
                {
                    FrameworkElement control = (FrameworkElement)listview.Items[index - 1];
                    ControlWrapper wrapper = this.getChildControlWrapper(control);

                    // Unregister any bindings for this element or any descendants
                    //
                    wrapper.Unregister();

                    listview.Items.RemoveAt(index - 1);
                }
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
                this.setListViewSelection(listview, "$data", _localSelection);
            }
        }

        public JToken getListViewSelection(ListView listview, string selectionItem)
        {
            if (listview.SelectionMode == ListViewSelectionMode.Multiple)
            {
                return new JArray(
                    from FrameworkElement control in listview.SelectedItems
                    select this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()
                );
            }
            else if (listview.SelectionMode == ListViewSelectionMode.Single)
            {
                FrameworkElement control = (FrameworkElement)listview.SelectedItem;
                if (control != null)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue().DeepClone();
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        public void setListViewSelection(ListView listview, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            if (listview.SelectionMode == ListViewSelectionMode.Multiple)
            {
                listview.SelectedItems.Clear();

                foreach (FrameworkElement control in listview.Items)
                {
                    if (selection is JArray)
                    {
                        JArray array = selection as JArray;
                        foreach (JToken item in array.Children())
                        {
                            if (JToken.DeepEquals(item, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                            {
                                listview.SelectedItems.Add(control);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (JToken.DeepEquals(selection, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                        {
                            listview.SelectedItems.Add(control);
                        }
                    }
                }
            }
            else if (listview.SelectionMode == ListViewSelectionMode.Single)
            {
                listview.SelectedItem = null;

                foreach (FrameworkElement control in listview.Items)
                {
                    if (JToken.DeepEquals(selection, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                    {
                        listview.SelectedItem = control;
                        break;
                    }
                }
            }

            _selectionChangingProgramatically = false;
        }

        void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Util.debug("Listbox selection changed");
            ListView listview = (ListView)sender;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getListViewSelection(listview, "$data");
            }

            if (!_selectionChangingProgramatically)
            {
                Util.debug("Listview selection changed by user!");
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    Util.debug("ListView selection changed with command: " + command);

                    if (listview.SelectionMode == ListViewSelectionMode.Single)
                    {
                        // For selection mode "Single", the command handler resolves its tokens relative to the item selected.
                        //
                        // There should always be a first "added" item, which represents the current selection (in single select).
                        //
                        if ((e.AddedItems != null) && (e.AddedItems.Count > 0))
                        {
                            ControlWrapper wrapper = this.getChildControlWrapper((FrameworkElement)e.AddedItems[0]);
                            if (wrapper != null)
                            {
                                StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                            }
                        }
                    }
                    else if (listview.SelectionMode == ListViewSelectionMode.Multiple)
                    {
                        // For selection mode "Multiple", the command hander resovles its tokens relative to the listview, not any list item(s).
                        //
                        StateManager.processCommand(command.Command, command.GetResolvedParameters(this.BindingContext));
                    }
                }
            }
        }

        void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // This will get called when the selection mode is "None" and an item is clicked (no selection change events will
            // fire in this case).
            //
            CommandInstance command = GetCommand(CommandName.OnItemClick);
            if (command != null)
            {
                Util.debug("ListView item click with command: " + command);

                // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                //
                ControlWrapper wrapper = this.getChildControlWrapper((FrameworkElement)e.ClickedItem);
                if (wrapper != null)
                {
                    StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                }
            }
        }
    }
}
