using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        FrameworkElement _content;

        public ListViewItem(FrameworkElement content, ListViewSelectionMode selectionMode, bool targetingRequired) : base()
        {
            _content = content;

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
                Grid.SetColumn(_content, 1);
            }

            _content.VerticalAlignment = VerticalAlignment.Center;
            this.Children.Add(_content);
        }

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
            LongListSelector listview = new LongListSelector();
            this._control = listview;

            listview.IsGroupingEnabled = false;

            listview.SelectionChanged += listview_SelectionChanged;
            listview.ItemRealized += listview_ItemRealized;
            listview.ItemUnrealized += listview_ItemUnrealized;

            applyFrameworkElementDefaults(listview);

            // Get selection mode - single (default) or multiple - no dynamic values (we don't need this changing during execution).
            if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "Multiple"))
            {
                _selectionMode = ListViewSelectionMode.Multiple;
            }
            else if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "None"))
            {
                _selectionMode = ListViewSelectionMode.None;
            }

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
                    /*
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

                     */
                }

                if (bindingSpec["selection"] != null)
                {
                    /*
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
                
                     */
                }
            }

            List<FrameworkElement> elements = new List<FrameworkElement>();
            elements.Add(CreateItem("Item One"));
            elements.Add(CreateItem("Item Two"));
            elements.Add(CreateItem("Item Three"));
            listview.ItemsSource = elements;
        }

        FrameworkElement CreateItem(string content)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = content;
            return new ListViewItem(textBlock, _selectionMode, _targetingRequired);
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
                    }
                    else if (_selectionMode == ListViewSelectionMode.Single)
                    {
                        if (item.CheckBox.IsChecked == false)
                        {
                            foreach(ListViewItem listItem in listview.ItemsSource)
                            {
                                if (listItem == item)
                                {
                                    listItem.CheckBox.IsChecked = true;
                                }
                                else
                                {
                                    if (listItem.CheckBox.IsChecked == true)
                                    {
                                        listItem.CheckBox.IsChecked = false;
                                    }
                                }
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
                    // !!! Store the wrapper for the item in the ListViewItem and get it back out here...
                    /*
                    ControlWrapper wrapper = this.getChildControlWrapper((FrameworkElement)e.ClickedItem);
                    if (wrapper != null)
                    {
                        StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                    }
                     */
                }

                listview.SelectedItem = null;
            }
        }

        void listview_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            Util.debug("ListView ItemRealized");
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                // e.Container is ContentPresenter
                // e.Container.Content - this is whatever was in the list...
            }
        }

        void listview_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            Util.debug("ListView ItemUnrealized");
        }
    }
}
