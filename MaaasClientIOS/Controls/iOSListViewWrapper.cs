using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    public class BindingContextTableViewCell : UITableViewCell
    {
        protected UIView _control;

        public UIView Control
        {
            get { return _control; }
            set
            {
                if (_control != null)
                {
                    _control.RemoveFromSuperview();
                }
                _control = value;
                this.AddSubview(_control);
            }
        }

        public BindingContextTableViewCell(NSString cellIdentifier)
            : base(UITableViewCellStyle.Default, cellIdentifier)
        {
        }
    }

    public class BindingContextTableSourceItem : TableSourceItem
    {
        static NSString _cellIdentifier = new NSString("ListViewCell");
        public override NSString CellIdentifier { get { return _cellIdentifier; } }

        protected iOSControlWrapper _parentControlWrapper;
        protected iOSControlWrapper _contentControlWrapper; // !!! Need to unregister when we go out of scope
        protected JObject _itemTemplate;

        protected BindingContext _bindingContext;
        public BindingContext BindingContext { get { return _bindingContext; } }

        public BindingContextTableSourceItem(iOSControlWrapper parentControl, JObject itemTemplate, BindingContext bindingContext)
        {
            _parentControlWrapper = parentControl;
            _itemTemplate = itemTemplate;
            _bindingContext = bindingContext;

            // Creating the content control here subverts the whole idea of being able to recycle cells.
            // We could maintain the content control for active cells by just creating/destroying them
            // as appropriate in BindCell.  The reason we aren't doing that is that we need to know the
            // cell height *before* the cell is created and bound (so we just create the contents for all
            // items in advance so we can measure them in GetHeightForRow).
            //
            // If the row height was the same for all items (perhaps specified in a rowHeight attribute, or
            // if we know somehow that the container control is a fixed height), then we could do this 
            // the optimal way.  Ideally, we could do it dynamically (the current way if row height is
            // unknown, and the more optimal way if it is known).  That way, for large lists you would
            // encourage fixed row height, but otherwise not worry about it.
            //
            _contentControlWrapper = iOSControlWrapper.CreateControl(_parentControlWrapper, _bindingContext, _itemTemplate);
            _contentControlWrapper.Control.LayoutSubviews();
        }

        public override UITableViewCell CreateCell(UITableView tableView)
        {
            return new BindingContextTableViewCell(CellIdentifier);
        }

        public override void BindCell(UITableView tableView, UITableViewCell cell)
        {
            BindingContextTableViewCell tableViewCell = (BindingContextTableViewCell)cell;            
            tableViewCell.Control = _contentControlWrapper.Control;
        }

        public override float GetHeightForRow(UITableView tableView)
        {
            // !!! There is no notification method to use to determing when the changes (updated via binding) might
            //     have changed the size of the control and thus should reload the row (getting the new height).
            //
            return _contentControlWrapper.Control.Frame.Height;
        }
    }

    public class CheckableBindingContextTableSource : CheckableTableSource
    {
        protected iOSControlWrapper _parentControl;
        protected JObject _itemTemplate;

        public CheckableBindingContextTableSource(iOSControlWrapper parentControl, JObject itemTemplate, SelectionMode selectionMode, Action OnSelectionChange)
            : base(selectionMode, OnSelectionChange)
        {
            _parentControl = parentControl;
            _itemTemplate = itemTemplate;
        }

        public void SetContents(BindingContext bindingContext, string itemSelector)
        {
            _tableItems.Clear();
            foreach (BindingContext itemBindingContext in bindingContext.SelectEach(itemSelector))
            {
                BindingContextTableSourceItem item = new BindingContextTableSourceItem(_parentControl, _itemTemplate, itemBindingContext);
                _tableItems.Add(new CheckableTableSourceItem(item, NSIndexPath.FromItemSection(_tableItems.Count, 0)));
            }
        }

        public void AddItem(BindingContext bindingContext, bool isChecked = false)
        {
            BindingContextTableSourceItem item = new BindingContextTableSourceItem(_parentControl, _itemTemplate, bindingContext);
            _tableItems.Add(new CheckableTableSourceItem(item, NSIndexPath.FromItemSection(_tableItems.Count, 0)));
        }
    }

    class iOSListViewWrapper : iOSControlWrapper
    {
        public iOSListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating list view element");

            var table = new UITableView();
            this._control = table;

            // This is better performance, but only works if all the rows are the same height and you know the height ahead of time...
            //
            // table.RowHeight = 100;

            // The "new style" reuse model doesn't seem to work with custom table cell implementations
            //
            // table.RegisterClassForCellReuse(typeof(TableCell), TableCell.CellIdentifier);

            SelectionMode selectionMode = ToSelectionMode((string)controlSpec["select"]);
            table.Source = new CheckableBindingContextTableSource(this, (JObject)controlSpec["itemTemplate"], selectionMode, listview_SelectionChanged);

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(table);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", new string[] { "onItemClick" });
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
                        () => getListViewContents(table),
                        value => this.setListViewContents(table, GetValueBinding("items").BindingContext, itemSelector));
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
                        () => getListViewSelection(table, selectionItem),
                        value => this.setListViewSelection(table, selectionItem, (JToken)value));
                }
            }
        }

        public JToken getListViewContents(UITableView tableView)
        {
            Util.debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(UITableView tableView, BindingContext bindingContext, string itemSelector)
        {
            Util.debug("Setting listview contents");

            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            // Keep track of currently selected item/items so we can restore after repopulating list
            JToken selection = getListViewSelection(tableView, itemSelector);

            int oldCount = tableSource.AllItems.Count;

            tableSource.SetContents(bindingContext, itemSelector);

            int newCount = tableSource.AllItems.Count;

            List<NSIndexPath> reloadRows = new List<NSIndexPath>();
            List<NSIndexPath> insertRows = new List<NSIndexPath>();
            List<NSIndexPath> deleteRows = new List<NSIndexPath>();

            int maxCount = Math.Max(newCount, oldCount);
            for (int i = 0; i < maxCount; i++)
            {
                NSIndexPath row = NSIndexPath.FromRowSection(i, 0);
                if (i < Math.Min(newCount, oldCount))
                {
                    reloadRows.Add(row);
                }
                else if (i < newCount)
                {
                    insertRows.Add(row);
                }
                else
                {
                    deleteRows.Add(row);
                }
            }

            tableView.BeginUpdates();
            if (reloadRows.Count > 0)
            {
                tableView.ReloadRows(reloadRows.ToArray(), UITableViewRowAnimation.Fade);
            }
            if (insertRows.Count > 0)
            {
                tableView.InsertRows(insertRows.ToArray(), UITableViewRowAnimation.Fade);
            }
            if (deleteRows.Count > 0)
            {
                tableView.DeleteRows(deleteRows.ToArray(), UITableViewRowAnimation.Fade);
            }
            tableView.EndUpdates(); // applies the changes

            setListViewSelection(tableView, itemSelector, selection);
        }

        public JToken getListViewSelection(UITableView tableView, string selectionItem)
        {
            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            List<CheckableTableSourceItem> checkedItems = tableSource.CheckedItems;

            if (tableSource.SelectionMode == SelectionMode.Multiple)
            {
                return new JArray(
                    from item in checkedItems
                    select ((BindingContextTableSourceItem)item.TableSourceItem).BindingContext.Select(selectionItem).GetValue()
                    );
            }
            else
            {
                if (checkedItems.Count > 0)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return ((BindingContextTableSourceItem)checkedItems[0].TableSourceItem).BindingContext.Select(selectionItem).GetValue().DeepClone();
                }
                return new JValue(false); // This is a "null" selection
            }
        }

        // This gets triggered when selection changes come in from the server (including when the selection is initially set),
        // and it also gets triggered when the list itself changes (including when the list contents are intially set).  So 
        // in the initial list/selection set case, this gets called twice.  On subsequent updates it's possible that this will
        // be triggered by either a list change or a selection change from the server, or both.  There is no easy way currerntly
        // to detect the "both" case (without exposing a lot more information here).  We're going to go ahead and live with the
        // multiple calls.  It shouldn't hurt anything (they should produce the same result), it's just slightly inefficient.
        // 
        public void setListViewSelection(UITableView tableView, string selectionItem, JToken selection)
        {
            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            // Go through all values and check as appropriate
            //
            foreach (CheckableTableSourceItem checkableItem in tableSource.AllItems)
            {
                bool isChecked = false;
                BindingContextTableSourceItem bindingItem = (BindingContextTableSourceItem)checkableItem.TableSourceItem;
                JToken boundValue = bindingItem.BindingContext.Select(selectionItem).GetValue();

                if (selection is JArray)
                {
                    JArray array = selection as JArray;
                    foreach (JToken item in array.Children())
                    {
                        if (JToken.DeepEquals(item, boundValue))
                        {
                            isChecked = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (JToken.DeepEquals(selection, boundValue))
                    {
                        isChecked = true;
                    }
                }

                checkableItem.SetChecked(tableView, isChecked);
            }
        }

        void listview_SelectionChanged()
        {
            updateValueBindingForAttribute("selection");
        }
    }
}