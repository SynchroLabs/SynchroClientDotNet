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
        string _item;

        public TextListViewItem(BindingContext bindingContext, string item, ListViewSelectionMode selectionMode, bool targetingRequired)
            : base(new TextBlock(), selectionMode, targetingRequired)
        {
            _bindingContext = bindingContext;
            _item = item;
            ((TextBlock)_control).Text = _bindingContext.Select(_item).GetValue().ToString();
        }

        public BindingContext BindingContext { get { return _bindingContext; } }

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
        ListViewSelectionMode _selectionMode = ListViewSelectionMode.Single;
        bool _targetingRequired = false;

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        public WinPhoneListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listbox element");
            LongListSelector listView = new LongListSelector();
            this._control = listView;

            listView.IsGroupingEnabled = false;

            listView.SelectionChanged += listview_SelectionChanged;

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

            listView.ItemsSource = new ListViewItems();

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

        public JToken getListViewContents(LongListSelector listview)
        {
            return new JArray(
                from item in (ListViewItems)listview.ItemsSource
                select new JValue(((TextListViewItem)item).GetValue())
                );
        }

        public void setListViewContents(LongListSelector listview, JObject itemTemplate, BindingContext bindingContext, string itemSelector)
        {
            Util.debug("Setting listview contents");

            _selectionChangingProgramatically = true;

            ListViewItems items = (ListViewItems)listview.ItemsSource;
            items.Clear();

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");
            foreach(BindingContext context in itemContexts)
            {
                items.Add(new TextListViewItem(context, itemSelector, _selectionMode, _targetingRequired));
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

        // This gets triggered when selection changes come in from the server (including when the selection is initially set),
        // and it also gets triggered when the list itself changes (including when the list contents are intially set).  So 
        // in the initial list/selection set case, this gets called twice.  On subsequent updates it's possible that this will
        // be triggered by either a list change or a selection change from the server, or both.  There is no easy way currerntly
        // to detect the "both" case (without exposing a lot more information here).  We're going to go ahead and live with the
        // multiple calls.  It shouldn't hurt anything (they should produce the same result), it's just slightly inefficient.
        // 
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
            // This is a way to implemented "item clicked"
            LongListSelector listview = sender as LongListSelector;
            if ((listview != null) && (listview.SelectedItem != null))
            {
                Util.debug("ListBox item clicked");
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

                CommandInstance command = GetCommand("onItemClick");
                if (command != null)
                {
                    Util.debug("ListView item click with command: " + command);

                    // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                    //
                    StateManager.processCommand(command.Command, command.GetResolvedParameters(item.BindingContext));
                }

                listview.SelectedItem = null;
            }
        }
    }
}
