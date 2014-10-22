using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;

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

        public CheckableBindingContextTableSource(iOSControlWrapper parentControl, JObject itemTemplate, ListSelectionMode selectionMode, OnSelectionChanged OnSelectionChanged, OnItemClicked OnItemClicked)
            : base(selectionMode, OnSelectionChanged, OnItemClicked)
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
        static Logger logger = Logger.GetLogger("iOSListViewWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public iOSListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating list view element");

            var table = new UITableView();
            this._control = table;

            // This is better performance, but only works if all the rows are the same height and you know the height ahead of time...
            //
            // table.RowHeight = 100;

            // The "new style" reuse model doesn't seem to work with custom table cell implementations
            //
            // table.RegisterClassForCellReuse(typeof(TableCell), TableCell.CellIdentifier);

            ListSelectionMode selectionMode = ToListSelectionMode(controlSpec["select"]);

            table.Source = new CheckableBindingContextTableSource(this, (JObject)controlSpec["itemTemplate"], selectionMode, listview_SelectionChanged, listview_ItemClicked);

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(table);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListViewContents(table),
                    value => this.setListViewContents(table, GetValueBinding("items").BindingContext));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListViewSelection(table, selectionItem),
                    value => this.setListViewSelection(table, selectionItem, (JToken)value));
            }
        }

        public JToken getListViewContents(UITableView tableView)
        {
            logger.Debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(UITableView tableView, BindingContext bindingContext)
        {
            logger.Debug("Setting listview contents");

            _selectionChangingProgramatically = true;

            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            int oldCount = tableSource.AllItems.Count;
            tableSource.SetContents(bindingContext, "$data");
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

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                selectionBinding.UpdateViewFromViewModel();
            }
            else if (_localSelection != null)
            {
                // If there is not a "selection" value binding, then we use local selection state to restore the selection when
                // re-filling the list.
                //
                this.setListViewSelection(tableView, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getListViewSelection(UITableView tableView, string selectionItem)
        {
            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            List<CheckableTableSourceItem> checkedItems = tableSource.CheckedItems;

            if (tableSource.SelectionMode == ListSelectionMode.Multiple)
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
            _selectionChangingProgramatically = true;

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

            _selectionChangingProgramatically = false;
        }

        void listview_ItemClicked(TableSourceItem itemClicked)
        {
            logger.Debug("Listview item clicked: {0}", itemClicked);

            UITableView tableView = (UITableView)this.Control;
            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            if (tableSource.SelectionMode == ListSelectionMode.None)
            {
                CommandInstance command = GetCommand(CommandName.OnItemClick);
                if (command != null)
                {
                    logger.Debug("ListView item click with command: {0}", command);

                    BindingContextTableSourceItem item = itemClicked as BindingContextTableSourceItem;
                    if (item != null)
                    {
                        // The item click command handler resolves its tokens relative to the item clicked.
                        //
                        Task t = StateManager.processCommand(command.Command, command.GetResolvedParameters(item.BindingContext));
                    }
                }
            }
        }

        void listview_SelectionChanged(TableSourceItem itemClicked)
        {
            logger.Debug("Listview selection changed");

            UITableView tableView = (UITableView)this.Control;
            CheckableBindingContextTableSource tableSource = (CheckableBindingContextTableSource)tableView.Source;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getListViewSelection(tableView, "$data");
            }

            if ((!_selectionChangingProgramatically) && (tableSource.SelectionMode != ListSelectionMode.None))
            {
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    logger.Debug("ListView selection change with command: {0}", command);

                    if (tableSource.SelectionMode == ListSelectionMode.Single)
                    {
                        BindingContextTableSourceItem item = itemClicked as BindingContextTableSourceItem;
                        if (item != null)
                        {
                            // The selection change command handler resolves its tokens relative to the item selected when in single select mode.
                            //
                            Task t = StateManager.processCommand(command.Command, command.GetResolvedParameters(item.BindingContext));
                        }
                    }
                    else if (tableSource.SelectionMode == ListSelectionMode.Multiple)
                    {
                        // The selection change command handler resolves its tokens relative to the list context when in multiple select mode.
                        //
                        Task t = StateManager.processCommand(command.Command, command.GetResolvedParameters(this.BindingContext));
                    }
                }
            }
        }
    }
}