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
    // See: 
    //
    //    http://docs.xamarin.com/guides/ios/user_interface/tables/
    //
    //    http://docs.xamarin.com/guides/ios/user_interface/tables/part_3_-_customizing_a_table's_appearance/

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
            Util.debug("Getting cell for: " + _indexPath);
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

    public delegate void OnItemClicked(TableSourceItem item);

    public class CheckableTableSource : UITableViewSource
    {
        protected List<CheckableTableSourceItem> _tableItems = new List<CheckableTableSourceItem>();

        protected Action _onSelectionChange;
        protected OnItemClicked _onItemClicked;
        protected SelectionMode _selectionMode;

        public CheckableTableSource(SelectionMode selectionMode, Action OnSelectionChange, OnItemClicked OnItemClicked = null)
        {
            _selectionMode = selectionMode;
            _onSelectionChange = OnSelectionChange;
            _onItemClicked = OnItemClicked;
        }

        public SelectionMode SelectionMode { get { return _selectionMode; } }

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
            Util.debug("Getting cell for path: " + indexPath);
            CheckableTableSourceItem item = _tableItems[indexPath.Row];
            UITableViewCell cell = item.GetCell(tableView);
            if ((_selectionMode == SelectionMode.None) && (_onItemClicked != null))
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
            Util.debug("Getting row height for: " + indexPath);
            CheckableTableSourceItem item = _tableItems[indexPath.Row];
            float height = item.GetHeightForRow(tableView);
            if (height == -1)
            {
                height = base.GetHeightForRow(tableView, indexPath);
            }
            return height;
        }


        public override void RowSelected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row selected: " + indexPath);

            tableView.DeselectRow(indexPath, true); // normal iOS behaviour is to remove the blue highlight

            CheckableTableSourceItem selectedItem = _tableItems[indexPath.Row];

            if ((_selectionMode == SelectionMode.Multiple) || ((_selectionMode == SelectionMode.Single) && !selectedItem.Checked))
            {
                if (_selectionMode == SelectionMode.Single)
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

                if (_onSelectionChange != null)
                {
                    _onSelectionChange();
                }
            }

            if (_onItemClicked != null)
            {
                _onItemClicked(selectedItem.TableSourceItem);
            }
        }

        public override void RowDeselected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row deselected: " + indexPath);
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

    public class StringTableSourceItem : TableSourceItem
    {
        static NSString _cellIdentifier = new NSString("StringTableCell");
        public override NSString CellIdentifier { get { return _cellIdentifier; } }

        protected string _string;
        public string String { get { return _string; } }

        public StringTableSourceItem(string str)
        {
            _string = str;
        }

        public override UITableViewCell CreateCell(UITableView tableView)
        {
            return new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier);
        }

        public override void BindCell(UITableView tableView, UITableViewCell cell)
        {
            cell.TextLabel.Text = _string;
            // cell.DetailTextLabel.Text = "Details";
            // cell.ImageView.Image
        }
    }

    public class CheckableStringTableSource : CheckableTableSource
    {
        public CheckableStringTableSource(SelectionMode selectionMode, Action OnSelectionChange)
            : base(selectionMode, OnSelectionChange)
        {
        }

        public void AddItem(string value, bool isChecked = false)
        {
            StringTableSourceItem item = new StringTableSourceItem(value);
            _tableItems.Add(new CheckableTableSourceItem(item, NSIndexPath.FromItemSection(_tableItems.Count, 0)));
        }
    }

    class iOSListBoxWrapper : iOSControlWrapper
    {
        public iOSListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating list box element");

            var table = new UITableView();
            this._control = table;

            // The "new style" reuse model doesn't seem to work with custom table cell implementations
            //
            // table.RegisterClassForCellReuse(typeof(TableCell), TableCell.CellIdentifier);

            SelectionMode selectionMode = ToSelectionMode((string)controlSpec["select"]);
            table.Source = new CheckableStringTableSource(selectionMode, listbox_SelectionChanged);

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(table);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items");
            if (bindingSpec != null)
            {
                if (bindingSpec["items"] != null)
                {
                    processElementBoundValue("items", (string)bindingSpec["items"], () => getListboxContents(table), value => this.setListboxContents(table, (JToken)value));
                }
                if (bindingSpec["selection"] != null)
                {
                    processElementBoundValue("selection", (string)bindingSpec["selection"], () => getListboxSelection(table), value => this.setListboxSelection(table, (JToken)value));
                }
            }
        }

        public JToken getListboxContents(UITableView tableView)
        {
            Util.debug("Getting listbox contents");

            CheckableStringTableSource tableSource = (CheckableStringTableSource)tableView.Source;

            return new JArray(
                from item in tableSource.AllItems
                select new Newtonsoft.Json.Linq.JValue(((StringTableSourceItem)item.TableSourceItem).String)
                );
        }

        public void setListboxContents(UITableView tableView, JToken contents)
        {
            Util.debug("Setting listbox contents");

            CheckableStringTableSource tableSource = (CheckableStringTableSource)tableView.Source;

            // Keep track of currently selected item/items so we can restore after repopulating list
            JToken selection = getListboxSelection(tableView);

            int oldCount = tableSource.AllItems.Count;
            tableSource.AllItems.Clear();

            if ((contents != null) && (contents.Type == JTokenType.Array))
            {
                // !!! Default itemValue is "$data"
                foreach (JToken arrayElementBindingContext in (JArray)contents)
                {
                    // !!! If $data (default), then we get the value of the binding context iteration items.
                    //     Otherwise, if there is a non-default itemData binding, we apply that.
                    string value = (string)arrayElementBindingContext;
                    Util.debug("adding listbox item: " + value);
                    tableSource.AddItem(value);
                }
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

            setListboxSelection(tableView, selection);
        }

        public JToken getListboxSelection(UITableView tableView)
        {
            CheckableStringTableSource tableSource = (CheckableStringTableSource)tableView.Source;

            List<CheckableTableSourceItem> checkedItems = tableSource.CheckedItems;

            if (tableSource.SelectionMode == SelectionMode.Multiple)
            {
                return new JArray(
                    from item in checkedItems
                    select new Newtonsoft.Json.Linq.JValue(((StringTableSourceItem)item.TableSourceItem).String)
                    );
            }
            else
            {
                if (checkedItems.Count > 0)
                {
                    return new JValue(((StringTableSourceItem)checkedItems[0].TableSourceItem).String);
                }
                return new JValue(false); // This is a "null" selection
            }
        }

        public void setListboxSelection(UITableView tableView, JToken selection)
        {
            CheckableStringTableSource tableSource = (CheckableStringTableSource)tableView.Source;

            // Get list of values to be selected...
            //
            List<string> selectedStrings = new List<string>();
            if (selection is JArray)
            {
                JArray array = selection as JArray;
                foreach (JToken item in array.Values())
                {
                    selectedStrings.Add(ToString(item));
                }
            }
            else
            {
                selectedStrings.Add(ToString(selection));
            }

            // Go through all values and check as appropriate
            //
            foreach (CheckableTableSourceItem item in tableSource.AllItems)
            {
                item.SetChecked(tableView, selectedStrings.Contains(((StringTableSourceItem)item.TableSourceItem).String));
            }
        }

        void listbox_SelectionChanged()
        {
            updateValueBindingForAttribute("selection");
        }
    }
}