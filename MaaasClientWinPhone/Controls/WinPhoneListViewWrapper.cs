using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaaasClientWinPhone.Controls
{
    // http://msdn.microsoft.com/en-us/library/system.windows.controls.contentpresenter(v=vs.110).aspx
    //

    public enum ListViewSelectionMode
    {
        None = 0,
        Single = 1,
        Multiple = 2,
    }

    class ListViewItem : Grid
    {
        CheckBox _check;
        WinPhoneControlWrapper _controlWrapper;

        public ListViewItem(WinPhoneControlWrapper controlWrapper, ListViewSelectionMode selectionMode, bool targetingRequired) : base()
        {
            _controlWrapper = controlWrapper;

            if ((selectionMode != ListViewSelectionMode.None) || (targetingRequired))
            {
                RowDefinition row = new RowDefinition();
                row.MinHeight = 44; // A minimum size for touch targeting
                this.RowDefinitions.Add(row);
            }

            if (selectionMode != ListViewSelectionMode.None)
            {
                // Create a two column grid, where the first column is autosized and the second is star (expands to fill available space)
                //
                ColumnDefinition col1 = new ColumnDefinition();
                col1.Width = GridLength.Auto;
                this.ColumnDefinitions.Add(col1);

                ColumnDefinition col2 = new ColumnDefinition();
                col2.Width = new GridLength(1, GridUnitType.Star);
                this.ColumnDefinitions.Add(col2);

                // Column zero is a checkbox and a border (to cover and block input to the checkbox).  The column will size to the checkbox,
                // and the border will size to the column, completely "covering" the checkbox.
                //
                _check = new CheckBox(); // Margin is fairly chunky, required for targeting - set to zero but is actually (12, 20, 12, 20)
                Border border = new Border();
                border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Background required to capture touch input (but transparent)

                this.Children.Add(_check);
                this.Children.Add(border);

                // Column one is where the listview contents goes...
                //
                Grid.SetColumn(_controlWrapper.Control, 1);
            }

            _controlWrapper.Control.VerticalAlignment = VerticalAlignment.Center;
            this.Children.Add(_controlWrapper.Control);
        }

        public WinPhoneControlWrapper ControlWrapper { get { return _controlWrapper; } }
        public CheckBox CheckBox { get { return _check; } }
    }

    class WinPhoneListViewWrapper : WinPhoneControlWrapper
    {
        ListViewSelectionMode _selectionMode = ListViewSelectionMode.Single;
        bool _targetingRequired = false;

        public WinPhoneListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listview element");
            LongListSelector listView = new LongListSelector();
            this._control = listView;

            listView.IsGroupingEnabled = false;

            listView.SelectionChanged += listview_SelectionChanged;
            listView.ItemRealized += listview_ItemRealized;
            listView.ItemUnrealized += listview_ItemUnrealized;

            applyFrameworkElementDefaults(listView);

            // Get selection mode - single (default) or multiple - no dynamic values (we don't need this changing during execution).
            if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "Multiple"))
            {
                _selectionMode = ListViewSelectionMode.Multiple;
            }
            else if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "None"))
            {
                _selectionMode = ListViewSelectionMode.None;
            }

            ObservableCollection<ListViewItem> items = new ObservableCollection<ListViewItem>();
            listView.ItemsSource = items;

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", new string[] { "onItemClick" });

            ProcessCommands(bindingSpec, new string[] { "onItemClick" });
            if (GetCommand("onItemClick") != null)
            {
                _targetingRequired = true;
            }

            if (bindingSpec != null)
            {
                if (bindingSpec["items"] != null)
                {
                    string itemSelector = (string)bindingSpec["item"];
                    if (itemSelector == null)
                    {
                        itemSelector = "$data";
                    }

                    // This is a little unusual, but what the list view needs to update its contents is actually not the "value" (which would
                    // be the array of content items based on the "items" binding), but the binding context representing where those values are
                    // in the view model.  It needs the binding context in part so that it can apply itemSelector to the iterated bindings to
                    // produce the actual item binding contexts.  And of course it also needs the itemSelector value.  So we'll pass both of those
                    // in the delegate (and ignore "value" altogether).  This is all perfectly fine, it's just not the normal "jam the value into
                    // the control" kind of delegate, so it seemed prudent to note that here.
                    //
                    processElementBoundValue(
                        "items",
                        (string)bindingSpec["items"],
                        () => getListViewContents(listView),
                        value => this.setListViewContents(listView, (JObject)controlSpec["itemTemplate"], GetValueBinding("items").BindingContext, itemSelector));
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
                        () => getListViewSelection(listView, selectionItem),
                        value => this.setListViewSelection(listView, selectionItem, (JToken)value));
                }
            }
        }

        public JToken getListViewContents(LongListSelector listbox)
        {
            Util.debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(LongListSelector listview, JObject itemTemplate, BindingContext bindingContext, string itemSelector)
        {
            Util.debug("Setting listview contents");

            List<BindingContext> itemContexts = bindingContext.SelectEach(itemSelector);

            Collection<ListViewItem> items = (Collection<ListViewItem>)listview.ItemsSource;

            if (items.Count < itemContexts.Count)
            {
                // New items are added (to the end of the list)
                //
                for (int index = items.Count; index < itemContexts.Count; index++)
                {
                    WinPhoneControlWrapper controlWrapper = CreateControl(this, itemContexts[index], itemTemplate);
                    items.Add(new ListViewItem(controlWrapper, _selectionMode, _targetingRequired));
                }
            }
            else if (items.Count > itemContexts.Count)
            {
                // Items need to be removed (from the end of the list)
                //
                for (int index = items.Count; index > itemContexts.Count; index--)
                {
                    ListViewItem listViewItem = (ListViewItem)items[index - 1];

                    // Unregister any bindings for this element or any descendants
                    //
                    listViewItem.ControlWrapper.Unregister();

                    items.RemoveAt(index - 1);
                }
            }

            // This notification that the list backing this view has changed happens after the underlying bound values, and thus the list
            // view items themselves, have been updated.  We need to maintain the selection state, but that is difficult as items may
            // have "moved" list positions without any specific notification (other than this broad notification that the list itself changed).
            //
            // To address this, we get the "selection" binding for this list view, if any, and force a view update to reset the selection
            // state from the view model whenever the list bound to the list view changes (and after we've processed any adds/removes above).
            //
            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                selectionBinding.UpdateViewFromViewModel();
            }
        }

        // To determine if an item should selected, get an item from the list, get the ElementMetaData.BindingContext.  Apply any
        // selectionItem to the binding context, resolve that and compare it to the selection (selectionItem will always be provided
        // here, and will default to "$data").
        //
        public JToken getListViewSelection(LongListSelector listview, string selectionItem)
        {
            if (_selectionMode == ListViewSelectionMode.Multiple)
            {
                List<ControlWrapper> selected = new List<ControlWrapper>();
                foreach (ListViewItem listItem in listview.ItemsSource)
                {
                    if (listItem.CheckBox.IsChecked == true)
                    {
                        selected.Add(listItem.ControlWrapper);
                    }
                }

                return new JArray(
                    from ControlWrapper controlWrapper in selected
                    select controlWrapper.BindingContext.Select(selectionItem).GetValue()
                );
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                ControlWrapper selected = null;
                foreach (ListViewItem listItem in listview.ItemsSource)
                {
                    if (listItem.CheckBox.IsChecked == true)
                    {
                        selected = listItem.ControlWrapper;
                        break;
                    }
                }

                if (selected != null)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return selected.BindingContext.Select(selectionItem).GetValue().DeepClone();
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        // This gets triggered when selection changes come in from the server (including when the selection is initially set),
        // and it also gets triggered when the list itself changes (including when the list contents are intially set).  So 
        // in the initial list/selection set case, this gets called twice.  On subsequent updates it's possible that this will
        // be triggered by either a list change or a selection change from the server, or both.  There is no easy way currerntly
        // to detect the "both" case (without exposing a lot more information here).  We're going to go ahead and live with the
        // multiple calls.  It shouldn't hurt anything (they should produce the same result), it's just slightly inefficient.
        // 
        public void setListViewSelection(LongListSelector listview, string selectionItem, JToken selection)
        {
            if (_selectionMode == ListViewSelectionMode.Multiple)
            {
                foreach (ListViewItem listItem in listview.ItemsSource)
                {
                    bool selected = false;

                    if (selection is JArray)
                    {
                        JArray array = selection as JArray;
                        foreach (JToken item in array.Children())
                        {
                            if (JToken.DeepEquals(item, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                            {
                                selected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (JToken.DeepEquals(selection, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                        {
                            selected = true;
                        }
                    }

                    listItem.CheckBox.IsChecked = selected;
                }
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                foreach (ListViewItem listItem in listview.ItemsSource)
                {
                    bool selected = false;

                    if (JToken.DeepEquals(selection, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                    {
                        selected = true;
                    }

                    listItem.CheckBox.IsChecked = selected;
                }
            }
        }

        void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is a way to implemented "item clicked"
            LongListSelector listview = sender as LongListSelector;
            if ((listview != null) && (listview.SelectedItem != null))
            {
                Util.debug("ListView item clicked");
                ListViewItem item = e.AddedItems[0] as ListViewItem;
                if (item != null)
                {
                    if (_selectionMode == ListViewSelectionMode.Multiple)
                    {
                        item.CheckBox.IsChecked = !item.CheckBox.IsChecked;
                        updateValueBindingForAttribute("selection");
                    }
                    else if (_selectionMode == ListViewSelectionMode.Single)
                    {
                        if (item.CheckBox.IsChecked == false)
                        {
                            bool selectionChanged = false;

                            foreach(ListViewItem listItem in listview.ItemsSource)
                            {
                                if (listItem == item)
                                {
                                    listItem.CheckBox.IsChecked = true;
                                    selectionChanged = true;
                                }
                                else
                                {
                                    if (listItem.CheckBox.IsChecked == true)
                                    {
                                        listItem.CheckBox.IsChecked = false;
                                    }
                                }
                            }

                            if (selectionChanged)
                            {
                                updateValueBindingForAttribute("selection");
                            }
                        }
                    }
                }

                CommandInstance command = GetCommand("onItemClick");
                if (command != null)
                {
                    Util.debug("ListView item click with command: " + command);

                    // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                    //
                    ControlWrapper wrapper = item.ControlWrapper;
                    if (wrapper != null)
                    {
                        StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                    }
                }

                listview.SelectedItem = null;
            }
        }

        void listview_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                ListViewItem listViewItem = (ListViewItem)e.Container.Content;
                Util.debug("ListView ItemRealized: " + listViewItem.ControlWrapper);
            }
        }

        void listview_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                ListViewItem listViewItem = (ListViewItem)e.Container.Content;
                Util.debug("ListView ItemUnrealized: " + listViewItem.ControlWrapper);
            }
        }
    }
}
