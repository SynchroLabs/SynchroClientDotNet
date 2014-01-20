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

    public class TableCell : UITableViewCell
    {
        static NSString _cellIdentifier = new NSString("TableCell");
        public static NSString CellIdentifier { get { return _cellIdentifier; } }

        public TableCell() : base(UITableViewCellStyle.Default, CellIdentifier)
        {
        }
    }

    // !!! It seems like we want to use the accessory checkmark to show "selection", and not the iOS selection
    //     mechanism (with the blue or gray background).  So this seems to mean that we'll need to track our
    //     own "selected" state (used to get/set selection, and to drive prescence of checkbox).
    //
    /*
    public class SelectableTableSourceItem
    {
        protected bool _selected = false;
        protected NSIndexPath _indexPath;

        public bool Selected 
        { 
            get { return _selected; } 
            set 
            { 
                if (_selected != value)
                {
                    _selected = value; 
                }
            } 
        }

        public SelectableTableSourceItem(NSIndexPath indexPath)
        {
            _indexPath = indexPath;
        }

        public UITableViewCell GetCell()
        {
            return null;
        }

        public void SelectionChanged(UITableView tableView, bool isSelected)
        {
            _selected = isSelected;
        }
    }
    */

    public class TableSource : UITableViewSource
    {
        public List<string> TableItems = new List<string>();

        protected Action _onSelectionChange;

        public TableSource(Action OnSelectionChange)
        {
            _onSelectionChange = OnSelectionChange;
        }

        public override int RowsInSection(UITableView tableview, int section)
        {
            return TableItems.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Getting cell for path: " + indexPath);

            TableCell cell = tableView.DequeueReusableCell(TableCell.CellIdentifier) as TableCell;
            if (cell == null)
            {
                cell = new TableCell();
            }

            // cell.SelectionStyle = UITableViewCellSelectionStyle.Blue;

            cell.TextLabel.Text = TableItems[indexPath.Row];
            // cell.DetailTextLabel.Text = "Details";
            // cell.ImageView.Image
            // cell.Accessory
            return cell;
        }

        public string GetStringAtRow(MonoTouch.Foundation.NSIndexPath indexPath)
        {
            if (indexPath.Section == 0)
            {
                return TableItems[indexPath.Row];
            }

            return null;
        }

        public override void RowSelected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row selected: " + indexPath);
            UITableViewCell cell = tableView.CellAt(indexPath);
            cell.Accessory = UITableViewCellAccessory.Checkmark;
            if (_onSelectionChange != null)
            {
                _onSelectionChange();
            }
        }

        public override void RowDeselected(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            Util.debug("Row deselected: " + indexPath);
            UITableViewCell cell = tableView.CellAt(indexPath);
            cell.Accessory = UITableViewCellAccessory.None;
            if (_onSelectionChange != null)
            {
                _onSelectionChange();
            }
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

    class iOSListBoxWrapper : iOSControlWrapper
    {
        public iOSListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating list box element");

            var table = new UITableView();
            this._control = table;

            //table.RegisterClassForCellReuse(typeof(TableCell), TableCell.CellIdentifier);
            table.Source = new TableSource(listbox_SelectionChanged);

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

            // Get selection mode - single (default) or multiple - no dynamic values (we don't need this changing during execution).
            if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "Multiple"))
            {
                table.AllowsMultipleSelection = true;
            }
        }

        public JToken getListboxContents(UITableView listbox)
        {
            Util.debug("Getting listbox contents");

            TableSource tableSource = (TableSource)listbox.Source;

            return new JArray(
                from item in tableSource.TableItems
                select new Newtonsoft.Json.Linq.JValue(item)
                );
        }

        public void setListboxContents(UITableView tableView, JToken contents)
        {
            Util.debug("Setting listbox contents");

            TableSource tableSource = (TableSource)tableView.Source;

            // Keep track of currently selected item/items so we can restore after repopulating list
            JToken selection = getListboxSelection(tableView);

            List<string> items = new List<string>();
            if ((contents != null) && (contents.Type == JTokenType.Array))
            {
                // !!! Default itemValue is "$data"
                foreach (JToken arrayElementBindingContext in (JArray)contents)
                {
                    // !!! If $data (default), then we get the value of the binding context iteration items.
                    //     Otherwise, if there is a non-default itemData binding, we apply that.
                    string value = (string)arrayElementBindingContext;
                    Util.debug("adding listbox item: " + value);
                    items.Add(value);
                }
            }
            List<string> oldItems = tableSource.TableItems;
            tableSource.TableItems = items;

            List<NSIndexPath> reloadRows = new List<NSIndexPath>();
            List<NSIndexPath> insertRows = new List<NSIndexPath>();
            List<NSIndexPath> deleteRows = new List<NSIndexPath>();

            int maxCount = Math.Max(items.Count, oldItems.Count);
            for (int i = 0; i < maxCount; i++)
            {
                NSIndexPath row = NSIndexPath.FromRowSection(i, 0);
                if (i < Math.Min(items.Count, oldItems.Count))
                {
                    reloadRows.Add(row);
                }
                else if (i < items.Count)
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
            TableSource tableSource = (TableSource)tableView.Source;

            if (tableView.AllowsMultipleSelection)
            {
                NSIndexPath[] selectedRows = tableView.IndexPathsForSelectedRows;
                if (selectedRows == null)
                {
                    selectedRows = new NSIndexPath[]{};
                }

                return new JArray(
                    from selectedRow in selectedRows
                    select new Newtonsoft.Json.Linq.JValue(tableSource.GetStringAtRow(selectedRow))
                    );
            }
            else
            {
                NSIndexPath selectedRow = tableView.IndexPathForSelectedRow;
                if (selectedRow != null)
                {
                    return new JValue(tableSource.GetStringAtRow(selectedRow));
                }
                return new JValue(false); // This is a "null" selection
            }
        }

        public void setListboxSelection(UITableView tableView, JToken selection)
        {
            // This version eliminates any unnecessary clearing and later resetting of selection on a given selected item,
            // unlike the brute force "clear all item selection, then set selection for selected items" approach.  Specifically,
            // with this method if the selection does not change, this will be a no-op (in terms of select/deselect).
            //
            // For long lists with small numbers of selections, it would be more efficient to just process the current selections
            // and new selected values (as opposed to going throught the whole list).  Maybe later.

            TableSource tableSource = (TableSource)tableView.Source;

            // Get current selected rows...
            //
            List<NSIndexPath> selectedRows = new List<NSIndexPath>();
            if (tableView.IndexPathsForSelectedRows != null)
            {
                selectedRows.AddRange(tableView.IndexPathsForSelectedRows);
            }

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

            // Go through the rows and select/deselect as appropriate...
            //
            int itemCount = tableSource.RowsInSection(tableView, 0);
            for (int i = 0; i < itemCount; i++)
            {
                NSIndexPath row = NSIndexPath.FromRowSection(i, 0);
                if (selectedStrings.Contains(tableSource.GetStringAtRow(row)))
                {
                    if (!selectedRows.Contains(row))
                    {
                        tableView.SelectRow(row, false, UITableViewScrollPosition.None);
                    }
                }
                else
                {
                    if (selectedRows.Contains(row))
                    {
                        tableView.DeselectRow(row, false);
                    }
                }
            }
        }

        // !!! Currently not called
        void listbox_SelectionChanged()
        {
            updateValueBindingForAttribute("selection");
        }
    }
}