using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Drawing;
using System.Threading.Tasks;

namespace MaaasClientIOS.Controls
{
    abstract public class TableSourceItem
    {
        abstract public NSString CellIdentifier { get; }

        abstract public UITableViewCell CreateCell(UITableView tableView);

        abstract public void BindCell(UITableView tableView, UITableViewCell cell);

        // Override this and return true if you set the checked state yourself
        //
        public virtual bool SetCheckedState(UITableView tableView, UITableViewCell cell, bool isChecked)
        {
            return false;
        }

        // Override this and return the row height if you want to set the row height yourself
        //
        public virtual float GetHeightForRow(UITableView tableView)
        {
            return -1;
        }
    }

    // We want to use the accessory checkmark to show "selection", and not the iOS selection
    // mechanism (with the blue or gray background).  That means we'll need to track our
    // own "checked" state and use that to drive prescence of checkbox.
    //
    public class CheckableTableSourceItem
    {
        static Logger logger = Logger.GetLogger("CheckableTableSourceItem");

        protected bool _checked = false;
        protected NSIndexPath _indexPath;
        protected TableSourceItem _tableSourceItem;

        public bool Checked { get { return _checked; } }

        public CheckableTableSourceItem(TableSourceItem tableSourceItem, NSIndexPath indexPath)
        {
            _tableSourceItem = tableSourceItem;
            _indexPath = indexPath;
        }

        public TableSourceItem TableSourceItem { get { return _tableSourceItem; } }

        public void SetChecked(UITableView tableView, bool isChecked)
        {
            if (_checked != isChecked)
            {
                _checked = isChecked;
                UITableViewCell cell = tableView.CellAt(_indexPath);
                if (cell != null)
                {
                    SetCheckedState(tableView, cell);
                }
            }
        }

        public void SetCheckedState(UITableView tableView, UITableViewCell cell)
        {
            if (!_tableSourceItem.SetCheckedState(tableView, cell, Checked))
            {
                cell.Accessory = _checked ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
            }
        }

        public UITableViewCell GetCell(UITableView tableView)
        {
            logger.Debug("Getting cell for: {0}", _indexPath);
            UITableViewCell cell = tableView.DequeueReusableCell(_tableSourceItem.CellIdentifier);
            if (cell == null)
            {
                cell = _tableSourceItem.CreateCell(tableView);
                // cell.SelectionStyle = UITableViewCellSelectionStyle.Blue;
            }

            _tableSourceItem.BindCell(tableView, cell);
            SetCheckedState(tableView, cell);

            return cell;
        }

        public virtual float GetHeightForRow(UITableView tableView)
        {
            return _tableSourceItem.GetHeightForRow(tableView);
        }
    }

    public delegate void OnSelectionChanged(TableSourceItem item);
    public delegate void OnItemClicked(TableSourceItem item);

    public class CheckableTableSource : UITableViewSource
    {
        static Logger logger = Logger.GetLogger("CheckableTableSource"); 
        
        protected List<CheckableTableSourceItem> _tableItems = new List<CheckableTableSourceItem>();

        protected OnSelectionChanged _onSelectionChanged;
        protected OnItemClicked _onItemClicked;
        protected ListSelectionMode _selectionMode;

        public CheckableTableSource(ListSelectionMode selectionMode, OnSelectionChanged OnSelectionChanged, OnItemClicked OnItemClicked)
        {
            _selectionMode = selectionMode;
            _onSelectionChanged = OnSelectionChanged;
            _onItemClicked = OnItemClicked;
        }

        public ListSelectionMode SelectionMode { get { return _selectionMode; } }

        public List<CheckableTableSourceItem> AllItems { get { return _tableItems; } }

        public List<CheckableTableSourceItem> CheckedItems
        {
            get
            {
                List<CheckableTableSourceItem> checkedItems = new List<CheckableTableSourceItem>();
                foreach (CheckableTableSourceItem item in _tableItems)
                {
                    if (item.Checked)
                    {
                        checkedItems.Add(item);
                    }
                }

                return checkedItems;
            }
        }

        public override int RowsInSection(UITableView tableview, int section)
        {
            return _tableItems.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            logger.Debug("Getting cell for path: {0}", indexPath);
            CheckableTableSourceItem item = _tableItems[indexPath.Row];
            UITableViewCell cell = item.GetCell(tableView);
            if ((_selectionMode == ListSelectionMode.None) && (_onItemClicked != null))
            {
                cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
            }
            return cell;
        }

        public CheckableTableSourceItem GetItemAtRow(MonoTouch.Foundation.NSIndexPath indexPath)
        {
            if (indexPath.Section == 0)
            {
                return _tableItems[indexPath.Row];
            }

            return null;
        }

        public override float GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            logger.Debug("Getting row height for: {0}", indexPath);
            CheckableTableSourceItem item = _tableItems[indexPath.Row];
            return item.GetHeightForRow(tableView);
        }

        public override void RowSelected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            logger.Debug("Row selected: {0}", indexPath);

            tableView.DeselectRow(indexPath, true); // normal iOS behaviour is to remove the blue highlight

            CheckableTableSourceItem selectedItem = _tableItems[indexPath.Row];

            if ((_selectionMode == ListSelectionMode.Multiple) || ((_selectionMode == ListSelectionMode.Single) && !selectedItem.Checked))
            {
                if (_selectionMode == ListSelectionMode.Single)
                {
                    // Uncheck any currently checked item(s) and check the item selected
                    //
                    foreach (CheckableTableSourceItem item in _tableItems)
                    {
                        if (item.Checked)
                        {
                            item.SetChecked(tableView, false);
                        }
                    }
                    selectedItem.SetChecked(tableView, true);
                }
                else
                {
                    // Toggle the checked state of the item selected
                    //
                    selectedItem.SetChecked(tableView, !selectedItem.Checked);
                }

                if (_onSelectionChanged != null)
                {
                    _onSelectionChanged(selectedItem.TableSourceItem);
                }
            }
            else if ((_selectionMode == ListSelectionMode.None) && (_onItemClicked != null))
            {
                _onItemClicked(selectedItem.TableSourceItem);
            }
        }

        public override void RowDeselected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            logger.Debug("Row deselected: {0}", indexPath);
        }

        /*
        public override void RowHighlighted(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row highlighted: " + indexPath);
        }

        public override void RowUnhighlighted(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row unhighlighted: " + indexPath);
        }
         */
    }

    public class BindingContextAsStringTableSourceItem : TableSourceItem
    {
        static NSString _cellIdentifier = new NSString("StringTableCell");
        public override NSString CellIdentifier { get { return _cellIdentifier; } }

        protected BindingContext _bindingContext;
        protected string _itemContent;

        public BindingContextAsStringTableSourceItem(BindingContext bindingContext, string itemContent)
        {
            _bindingContext = bindingContext;
            _itemContent = itemContent;
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

        public override UITableViewCell CreateCell(UITableView tableView)
        {
            return new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier);
        }

        public override void BindCell(UITableView tableView, UITableViewCell cell)
        {
            cell.TextLabel.Text = this.ToString();
        }
    }

    public class BindingContextAsCheckableStringTableSource : CheckableTableSource
    {
        public BindingContextAsCheckableStringTableSource(ListSelectionMode selectionMode, OnSelectionChanged OnSelectionChanged, OnItemClicked OnItemClicked)
            : base(selectionMode, OnSelectionChanged, OnItemClicked)
        {
        }

        public void AddItem(BindingContext bindingContext, string itemContent, bool isChecked = false)
        {
            BindingContextAsStringTableSourceItem item = new BindingContextAsStringTableSourceItem(bindingContext, itemContent);
            _tableItems.Add(new CheckableTableSourceItem(item, NSIndexPath.FromItemSection(_tableItems.Count, 0)));
        }
    }

    class iOSListBoxWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSListBoxWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick.Attribute, CommandName.OnSelectionChange.Attribute };

        public iOSListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating list box element");

            var table = new UITableView();
            this._control = table;

            // The "new style" reuse model doesn't seem to work with custom table cell implementations
            //
            // table.RegisterClassForCellReuse(typeof(TableCell), TableCell.CellIdentifier);

            ListSelectionMode selectionMode = ToListSelectionMode(controlSpec["select"]);
            table.Source = new BindingContextAsCheckableStringTableSource(selectionMode, listbox_SelectionChanged, listbox_ItemClicked);

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(table);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListboxContents(table),
                    value => this.setListboxContents(table, GetValueBinding("items").BindingContext, itemContent));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListboxSelection(table, selectionItem),
                    value => this.setListboxSelection(table, selectionItem, (JToken)value));
            }
        }

        public JToken getListboxContents(UITableView tableView)
        {
            logger.Debug("Get listbox contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListboxContents(UITableView tableView, BindingContext bindingContext, string itemContent)
        {
            logger.Debug("Setting listbox contents");

            _selectionChangingProgramatically = true;

            BindingContextAsCheckableStringTableSource tableSource = (BindingContextAsCheckableStringTableSource)tableView.Source;

            int oldCount = tableSource.AllItems.Count;
            tableSource.AllItems.Clear();

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");
            foreach (BindingContext itemContext in itemContexts)
            {
                tableSource.AddItem(itemContext, itemContent);
            }

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
                this.setListboxSelection(tableView, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getListboxSelection(UITableView tableView, string selectionItem)
        {
            BindingContextAsCheckableStringTableSource tableSource = (BindingContextAsCheckableStringTableSource)tableView.Source;

            List<CheckableTableSourceItem> checkedItems = tableSource.CheckedItems;

            if (tableSource.SelectionMode == ListSelectionMode.Multiple)
            {
                JArray array = new JArray();
                foreach (var item in checkedItems)
                {
                    array.Add(((BindingContextAsStringTableSourceItem)item.TableSourceItem).GetSelection(selectionItem));
                }
                return array;
            }
            else
            {
                if (checkedItems.Count > 0)
                {
                    return ((BindingContextAsStringTableSourceItem)checkedItems[0].TableSourceItem).GetSelection(selectionItem);
                }
                return new JValue(false); // This is a "null" selection
            }
        }

        public void setListboxSelection(UITableView tableView, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            BindingContextAsCheckableStringTableSource tableSource = (BindingContextAsCheckableStringTableSource)tableView.Source;

            // Go through all values and check as appropriate
            //
            foreach (CheckableTableSourceItem tableSourceItem in tableSource.AllItems)
            {
                BindingContextAsStringTableSourceItem listItem = (BindingContextAsStringTableSourceItem)tableSourceItem.TableSourceItem;

                bool itemChecked = false;

                if (selection is JArray)
                {
                    JArray array = selection as JArray;
                    foreach (JToken item in array)
                    {
                        if (JToken.DeepEquals(item, listItem.GetSelection(selectionItem)))
                        {
                            tableSourceItem.SetChecked(tableView, true);
                            itemChecked = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (JToken.DeepEquals(selection, listItem.GetSelection(selectionItem)))
                    {
                        tableSourceItem.SetChecked(tableView, true);
                        itemChecked = true;
                    }
                }

                if (!itemChecked)
                {
                    tableSourceItem.SetChecked(tableView, false);
                }
            }

            _selectionChangingProgramatically = false;
        }

        async void listbox_ItemClicked(TableSourceItem item)
        {
            logger.Debug("Listbox item clicked");

            UITableView tableView = (UITableView)this.Control;
            BindingContextAsCheckableStringTableSource tableSource = (BindingContextAsCheckableStringTableSource)tableView.Source;

            if (tableSource.SelectionMode == ListSelectionMode.None)
            {
                BindingContextAsStringTableSourceItem listItem = item as BindingContextAsStringTableSourceItem;
                if (item != null)
                {
                    CommandInstance command = GetCommand(CommandName.OnItemClick);
                    if (command != null)
                    {
                        logger.Debug("ListBox item click with command: {0}", command);

                        // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                        //
                        await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                    }
                }
            }
        }

        async void listbox_SelectionChanged(TableSourceItem item)
        {
            logger.Debug("Listbox selection changed");

            UITableView tableView = (UITableView)this.Control;
            BindingContextAsCheckableStringTableSource tableSource = (BindingContextAsCheckableStringTableSource)tableView.Source;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getListboxSelection(tableView, "$data");
            }

            if ((!_selectionChangingProgramatically) && (tableSource.SelectionMode != ListSelectionMode.None))
            {
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    logger.Debug("ListView selection change with command: {0}", command);

                    if (tableSource.SelectionMode == ListSelectionMode.Single)
                    {
                        BindingContextAsStringTableSourceItem listItem = item as BindingContextAsStringTableSourceItem;
                        if (listItem != null)
                        {
                            // The selection change command handler resolves its tokens relative to the item selected when in single select mode.
                            //
                            await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                        }
                    }
                    else if (tableSource.SelectionMode == ListSelectionMode.Multiple)
                    {
                        // The selection change command handler resolves its tokens relative to the list context when in multiple select mode.
                        //
                        await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(this.BindingContext));
                    }
                }
            }
        }
    }
}