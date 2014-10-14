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
    public class TextListViewItem : ListViewItem
    {
        BindingContext _bindingContext;
        string _itemContent;

        public TextListViewItem(BindingContext bindingContext, string itemContent, ListViewSelectionMode selectionMode, bool targetingRequired)
            : base(new TextBlock(), selectionMode, targetingRequired)
        {
            _bindingContext = bindingContext;
            _itemContent = itemContent;
            ((TextBlock)_control).Text = this.ToString();
        }

        public BindingContext BindingContext { get { return _bindingContext; } }

        public override string ToString()
        {
            return PropertyValue.ExpandAsString(_itemContent, _bindingContext);
        }

        public JToken GetValue()
        {
            return _bindingContext.Select("$data").GetValue();
        }

        public JToken GetSelection(string selectionItem)
        {
            return _bindingContext.Select(selectionItem).GetValue().DeepClone();
        }

        //public string Text { get { return ((TextBlock)_control).Text; } }
    }

    class WinPhoneListBoxWrapper : WinPhoneControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPhoneListBoxWrapper");

        ListViewSelectionMode _selectionMode = ListViewSelectionMode.None;
        bool _targetingRequired = false;

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public WinPhoneListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating listbox element");

            LongListSelector listView = new LongListSelector();
            this._control = listView;

            listView.IsGroupingEnabled = false;

            listView.SelectionChanged += listview_SelectionChanged;

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
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListViewContents(listView),
                    value => this.setListViewContents(listView, (JObject)controlSpec["itemTemplate"], GetValueBinding("items").BindingContext, itemContent));
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

        public JToken getListViewContents(LongListSelector listview)
        {
            return new JArray(
                from item in (ListViewItems)listview.ItemsSource
                select new JValue(((TextListViewItem)item).GetValue())
                );
        }

        public void setListViewContents(LongListSelector listview, JObject itemTemplate, BindingContext bindingContext, string itemContent)
        {
            logger.Debug("Setting listview contents");

            _selectionChangingProgramatically = true;

            ListViewItems items = (ListViewItems)listview.ItemsSource;
            items.Clear();

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");
            foreach(BindingContext context in itemContexts)
            {
                items.Add(new TextListViewItem(context, itemContent, _selectionMode, _targetingRequired));
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
                    from TextListViewItem selectedItem in items.SelectedItems
                    select selectedItem.GetSelection(selectionItem)
                );
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                TextListViewItem selectedItem = items.SelectedItem as TextListViewItem;
                if (selectedItem != null)
                {
                    return selectedItem.GetSelection(selectionItem);
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        public void setListViewSelection(LongListSelector listview, string selectionItem, JToken selection)
        {
            ListViewItems items = (ListViewItems)listview.ItemsSource;

            if (_selectionMode == ListViewSelectionMode.Multiple)
            {
                items.SelectedItems.Clear();

                foreach (TextListViewItem listItem in listview.ItemsSource)
                {
                    if (selection is JArray)
                    {
                        JArray array = selection as JArray;
                        foreach (JToken item in array.Children())
                        {
                            if (JToken.DeepEquals(item, listItem.GetSelection(selectionItem)))
                            {
                                items.SelectedItems.Add(listItem);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (JToken.DeepEquals(selection, listItem.GetSelection(selectionItem)))
                        {
                            items.SelectedItems.Add(listItem);
                        }
                    }
                }
            }
            else if (_selectionMode == ListViewSelectionMode.Single)
            {
                foreach (TextListViewItem listItem in listview.ItemsSource)
                {
                    if (JToken.DeepEquals(selection, listItem.GetSelection(selectionItem)))
                    {
                        items.SelectedItem = listItem;
                    }
                }
            }
        }

        void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is how we implement "item clicked"
            LongListSelector listview = sender as LongListSelector;
            if ((listview != null) && (listview.SelectedItem != null))
            {
                logger.Debug("ListBox item clicked");
                TextListViewItem item = e.AddedItems[0] as TextListViewItem;
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
                                StateManager.processCommand(command.Command, command.GetResolvedParameters(item.BindingContext));
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
                                    StateManager.processCommand(command.Command, command.GetResolvedParameters(item.BindingContext));
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
    }
}
