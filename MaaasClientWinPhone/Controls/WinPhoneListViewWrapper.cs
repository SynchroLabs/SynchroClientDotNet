using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    public class ListViewItem : Grid
    {
        static Logger logger = Logger.GetLogger("ListViewItem");

        protected FrameworkElement _control;
        protected CheckBox _check;

        public ListViewItem(FrameworkElement control, ListViewSelectionMode selectionMode, bool targetingRequired) : base()
        {
            _control = control;

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
                Grid.SetColumn(_control, 1);
            }

            _control.VerticalAlignment = VerticalAlignment.Center;
            this.Children.Add(_control);
        }

        public bool Selected
        {
            get { return _check.IsChecked == true; }
            set 
            { 
                if (_check.IsChecked != value)
                {
                    _check.IsChecked = value;
                }
            }
        }
    }

    public class ControlWrapperListViewItem : ListViewItem
    {
        WinPhoneControlWrapper _controlWrapper;

        public ControlWrapperListViewItem(WinPhoneControlWrapper controlWrapper, ListViewSelectionMode selectionMode, bool targetingRequired) 
            : base(controlWrapper.Control, selectionMode, targetingRequired)
        {
            _controlWrapper = controlWrapper;
        }

        public WinPhoneControlWrapper ControlWrapper { get { return _controlWrapper; } }
    }

    public class SelectedListViewItems : ObservableCollection<ListViewItem>
    {
        public SelectedListViewItems() : base()
        {
        }

        // The Clear() method produces a "Reset" change event that does not contain any reference to the
        // items that were removed.  Since we need to deselect items that are removed, we will override
        // this behavior and manufactor a "Remove" change event with all items in the list.
        //
        protected override void ClearItems()
        {
            List<ListViewItem> removed = new List<ListViewItem>(this);
            base.ClearItems();
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
                base.OnCollectionChanged(e);
        }
    }

    public class ListViewItems : ObservableCollection<ListViewItem>
    {
        static Logger logger = Logger.GetLogger("ListViewItems");

        protected ListViewSelectionMode _selectionMode;
        protected SelectedListViewItems _selected = new SelectedListViewItems();

        public ListViewItems(ListViewSelectionMode selectionMode = ListViewSelectionMode.Single) : base()
        {
            _selectionMode = selectionMode;
            _selected.CollectionChanged += SelectedListViewItems_CollectionChanged;
        }

        public ListViewItem SelectedItem 
        { 
            get 
            { 
                if (_selected.Count > 0)
                {
                    return _selected[0];
                }
                return null; 
            } 

            set
            {
                _selected.Clear();
                _selected.Add(value);
            }
        }

        public Collection<ListViewItem> SelectedItems { get { return _selected; } }

        void SelectedListViewItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ListViewItem item in e.NewItems)
                {
                    logger.Debug("Selected ListViewItem added: {0}", item);
                    item.Selected = true;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ListViewItem item in e.OldItems)
                {
                    logger.Debug("Selected ListViewItem removed: {0}", item);
                    item.Selected = false;
                }
            }
        }
    }

    class WinPhoneListViewWrapper : WinPhoneControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPhoneListViewWrapper");

        ListViewSelectionMode _selectionMode = ListViewSelectionMode.None;
        bool _targetingRequired = false;

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public WinPhoneListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating listview element");
            LongListSelector listView = new LongListSelector();
            this._control = listView;

            listView.IsGroupingEnabled = false;

            listView.SelectionChanged += listview_SelectionChanged;
            listView.ItemRealized += listview_ItemRealized;
            listView.ItemUnrealized += listview_ItemUnrealized;

            applyFrameworkElementDefaults(listView);

            ListSelectionMode mode = ToListSelectionMode(controlSpec["select"]);
            switch (mode)
            {
                case ListSelectionMode.Single:
                    _selectionMode = ListViewSelectionMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    _selectionMode = ListViewSelectionMode.Multiple;
                    break;
            }

            listView.ItemsSource = new ListViewItems();

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnItemClick) != null)
            {
                _targetingRequired = true;
            }

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
        }

        public JToken getListViewContents(LongListSelector listbox)
        {
            logger.Debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(LongListSelector listview, JObject itemTemplate, BindingContext bindingContext)
        {
            logger.Debug("Setting listview contents");

            _selectionChangingProgramatically = true;

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");

            ListViewItems items = (ListViewItems)listview.ItemsSource;

            if (items.Count < itemContexts.Count)
            {
                // New items are added (to the end of the list)
                //
                for (int index = items.Count; index < itemContexts.Count; index++)
                {
                    WinPhoneControlWrapper controlWrapper = CreateControl(this, itemContexts[index], itemTemplate);
                    items.Add(new ControlWrapperListViewItem(controlWrapper, _selectionMode, _targetingRequired));
                }
            }
            else if (items.Count > itemContexts.Count)
            {
                // Items need to be removed (from the end of the list)
                //
                for (int index = items.Count; index > itemContexts.Count; index--)
                {
                    ControlWrapperListViewItem listViewItem = (ControlWrapperListViewItem)items[index - 1];

                    // Unregister any bindings for this element or any descendants
                    //
                    listViewItem.ControlWrapper.Unregister();

                    items.RemoveAt(index - 1);
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

            _selectionChangingProgramatically = false;
        }

        public JToken getListViewSelection(LongListSelector listview, string selectionItem)
        {
            ListViewItems items = (ListViewItems)listview.ItemsSource;

            if (_selectionMode == ListViewSelectionMode.Multiple)
            {
                return new JArray(
                    from ControlWrapperListViewItem controlWrapperItem in items.SelectedItems
                    select controlWrapperItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()
                );
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                ControlWrapperListViewItem selectedItem = items.SelectedItem as ControlWrapperListViewItem;
                if (selectedItem != null)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return selectedItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue().DeepClone();
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        public void setListViewSelection(LongListSelector listview, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            ListViewItems items = (ListViewItems)listview.ItemsSource;

            if (_selectionMode == ListViewSelectionMode.Multiple)
            {
                items.SelectedItems.Clear();

                foreach (ControlWrapperListViewItem listItem in listview.ItemsSource)
                {
                    if (selection is JArray)
                    {
                        JArray array = selection as JArray;
                        foreach (JToken item in array.Children())
                        {
                            if (JToken.DeepEquals(item, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                            {
                                items.SelectedItems.Add(listItem);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (JToken.DeepEquals(selection, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                        {
                            items.SelectedItems.Add(listItem);
                        }
                    }
                }
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                foreach (ControlWrapperListViewItem listItem in listview.ItemsSource)
                {
                    if (JToken.DeepEquals(selection, listItem.ControlWrapper.BindingContext.Select(selectionItem).GetValue()))
                    {
                        items.SelectedItem = listItem;
                    }
                }
            }

            _selectionChangingProgramatically = false;
        }

        void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is how we implement "item clicked"
            LongListSelector listview = sender as LongListSelector;
            if ((listview != null) && (listview.SelectedItem != null))
            {
                logger.Debug("ListView item clicked");
                ControlWrapperListViewItem item = e.AddedItems[0] as ControlWrapperListViewItem;
                if (item != null)
                {
                    ListViewItems items = (ListViewItems)listview.ItemsSource;

                    if (_selectionMode == ListViewSelectionMode.Multiple)
                    {
                        if (items.SelectedItems.Contains(item))
                        {
                            items.SelectedItems.Remove(item);
                        }
                        else
                        {
                            items.SelectedItems.Add(item);
                        }
                    }
                    else if (_selectionMode == ListViewSelectionMode.Single)
                    {
                        if (!item.Selected)
                        {
                            items.SelectedItem = item;
                        }
                    }
                }

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
                    // Process commands
                    //
                    if (_selectionMode == ListViewSelectionMode.None)
                    {
                        CommandInstance command = GetCommand(CommandName.OnItemClick);
                        if (command != null)
                        {
                            logger.Debug("ListView item clicked with command: {0}", command);

                            if (item != null)
                            {
                                // The item click command handler resolves its tokens relative to the item clicked.
                                //
                                ControlWrapper wrapper = item.ControlWrapper;
                                if (wrapper != null)
                                {
                                    StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                                }
                            }
                        }
                    }
                    else
                    {
                        CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                        if (command != null)
                        {
                            logger.Debug("ListView selection changed with command: {0}", command);

                            if (_selectionMode == ListViewSelectionMode.Single)
                            {
                                if (item != null)
                                {
                                    // The selection change command handler resolves its tokens relative to the item selected when in single select mode.
                                    //
                                    ControlWrapper wrapper = item.ControlWrapper;
                                    if (wrapper != null)
                                    {
                                        StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                                    }
                                }
                            }
                            else // ListViewSelectionMode.Multiple
                            {
                                // The selection change command handler resolves its tokens relative to the list context when in multiple select mode.
                                //
                                StateManager.processCommand(command.Command, command.GetResolvedParameters(this.BindingContext));
                            }
                        }
                    }
                }

                listview.SelectedItem = null;
            }
        }

        //
        // !!! We could use a ListViewItem that had a BindingContext and enough information to create the control(s)
        //     as needed.  Then the control conent could be created and added during ItemRealized, and removed/unregistered
        //     during ItemUnrealized.  It wouldn't be a pure virtual list, but it might be better than what we have no
        //     for large lists.
        //

        void listview_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                ControlWrapperListViewItem listViewItem = (ControlWrapperListViewItem)e.Container.Content;
                logger.Debug("ListView ItemRealized: {0}", listViewItem.ControlWrapper);
            }
        }

        void listview_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                ControlWrapperListViewItem listViewItem = (ControlWrapperListViewItem)e.Container.Content;
                logger.Debug("ListView ItemUnrealized: {0}", listViewItem.ControlWrapper);
            }
        }
    }
}
