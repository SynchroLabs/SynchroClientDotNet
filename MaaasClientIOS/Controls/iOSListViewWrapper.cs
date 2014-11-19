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
        static Logger logger = Logger.GetLogger("BindingContextTableViewCell");

        protected void updateCellWidth()
        {
            if (_controlWrapper != null)
            {
                if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent)
                {
                    RectangleF cellBounds = _controlWrapper.Control.Frame;
                    cellBounds.Width = this.Frame.Width;
                    _controlWrapper.Control.Frame = cellBounds;
                    _controlWrapper.Control.LayoutSubviews();
                    // logger.Info("Sized 'fill parent width' cell width, now sized at: {0}", _controlWrapper.Control.Frame);
                }
            }
        }

        protected iOSControlWrapper _controlWrapper;
        public iOSControlWrapper ControlWrapper
        {
            get { return _controlWrapper; }
            set
            {
                if (_controlWrapper != value)
                {
                    if (_controlWrapper != null)
                    {
                        // Remove any control currently set into this cell...
                        //
                        _controlWrapper.Control.RemoveFromSuperview();
                    }
                    _controlWrapper = value;
                    if (_controlWrapper != null)
                    {
                        if (_controlWrapper.Control.Superview != null)
                        {
                            // If the control we're setting in this cell is still a child of something (presumably
                            // the cell to which it was previously assigned), we need to remove it from that parent
                            // before we assign it as our child (otherwise there are cases where a cell will end up
                            // with either zero or more than one set of controls, depending on the iOS version).
                            //
                            _controlWrapper.Control.RemoveFromSuperview();                            
                        }
                        this.AddSubview(_controlWrapper.Control);
                        updateCellWidth();
                    }
                }
            }
        }

        public BindingContextTableViewCell(NSString cellIdentifier)
            : base(UITableViewCellStyle.Default, cellIdentifier)
        {
        }

        public override void LayoutSubviews()
        {
            updateCellWidth();
            base.LayoutSubviews();
        }
    }

    public class BindingContextTableSourceItem : TableSourceItem
    {
        static Logger logger = Logger.GetLogger("BindingContextTableSourceItem");

        static NSString _cellIdentifier = new NSString("ListViewCell");
        public override NSString CellIdentifier { get { return _cellIdentifier; } }

        protected iOSControlWrapper _parentControlWrapper;
        protected iOSControlWrapper _contentControlWrapper;
        protected JObject _itemTemplate;

        protected BindingContext _bindingContext;
        public BindingContext BindingContext { get { return _bindingContext; } }

        public BindingContextTableSourceItem(iOSControlWrapper parentControl, JObject itemTemplate, BindingContext bindingContext)
        {
            _parentControlWrapper = parentControl;
            _itemTemplate = itemTemplate;
            _bindingContext = bindingContext;

            // Creating the content control here subverts the whole idea of recycling cells (we are technically
            // recycling the cells themselves, but we are maintaining the contents of the cells, a bunch of bound
            // controls, which is kind of against the spirit of supporting large lists without chewing up lots of
            // resources).
            //
            // What we should do is create the bound (in the Synchro sense) controls when this item is bound
            // to a cell.  Since we are going to be asked for the height of the cell later, and since that can
            // change once we bind the controls to a cell and the cell lays them out, we need to keep a reference
            // to the controls around (so we can see how tall they are at any time).
            //
            // But in order to really "recycle" the cells, we should remove that reference when this item becomes
            // "unbound" from the cell to which it is bound (at which point we, coincidentally, don't need to know
            // the height anymore).  This would entail doing an Unregister() on the content control wrapper, then
            // nulling out the reference to it.  The probelm is that we don't really get an unbinding notification,
            // and the only way to get that is to have the BindingContentTableViewCell keep track of what is bound
            // to it, such that when something else gets bound to it, it can notify the thing that it is going to
            // unbind.  That's an exercise for later.
            //
            // If the row height was the same for all items (perhaps specified in a rowHeight attribute, or if we
            // know somehow that the container control is a fixed height for all rows), then we could do this a much
            // more optimal way (we could create/assign the content control on BindCell, hand it to the cell, keep
            // no reference, and let the cell Unregister it directly when it was done with it).
            //
            _contentControlWrapper = iOSControlWrapper.CreateControl(_parentControlWrapper, _bindingContext, _itemTemplate);
            _contentControlWrapper.Control.LayoutSubviews();
            // logger.Info("Creating new item, with frame after layout: {0}", _contentControlWrapper.Control.Frame);
        }

        public override UITableViewCell CreateCell(UITableView tableView)
        {
            return new BindingContextTableViewCell(CellIdentifier);
        }

        // Note that it is not uncommon to get a request to bind this item to a cell to which it has already been
        // most recently bound.  Check for and handle this case as appropriate.
        //
        public override void BindCell(UITableView tableView, UITableViewCell cell)
        {
            BindingContextTableViewCell tableViewCell = (BindingContextTableViewCell)cell;            
            tableViewCell.ControlWrapper = _contentControlWrapper;
        }

        public override float GetHeightForRow(UITableView tableView)
        {
            // !!! There is no notification method to use to determine when the changes (updated via binding) might
            //     have changed the size of the control and thus should reload the row (getting the new height).
            //
            // logger.Info("Returning row height of: {0}", _contentControlWrapper.Control.Frame.Height);
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

    abstract class TableContainerView : UIView
    {
        static Logger logger = Logger.GetLogger("ContainerView");

        protected UITableView _tableView;
        protected iOSControlWrapper _controlWrapper;

        public TableContainerView(UITableView tableView, iOSControlWrapper controlWrapper)
            : base()
        {
            this._tableView = tableView;
            this._controlWrapper = controlWrapper;

            // For explicit (static) child dimensions - size parent UIView to fit...
            //
            SizeF panelSize = this.Frame.Size;
            UIEdgeInsets margin = _controlWrapper.Margin;

            if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.Explicit)
            {
                panelSize.Width = _controlWrapper.Control.Frame.Width + margin.Left + margin.Right;
            }
            if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.Explicit)
            {
                panelSize.Height = _controlWrapper.Control.Frame.Height + margin.Top + margin.Bottom;
            }
            if ((panelSize.Width != this.Frame.Size.Width) || (panelSize.Height != this.Frame.Height))
            {
                RectangleF panelFrame = this.Frame;
                panelFrame.Size = panelSize;
                this.Frame = panelFrame;
            }

            base.AddSubview(_controlWrapper.Control);
            this.LayoutSubviews();
        }

        public abstract void UpdateSize();

        public override void LayoutSubviews()
        {
            // logger.Info("LayoutSubviews");

            if (_controlWrapper != null)
            {
                SizeF panelSize = this.Frame.Size;

                UIView childView = _controlWrapper.Control;
                if (childView.Hidden)
                {
                    panelSize.Height = 0;
                }
                else
                {
                    RectangleF childFrame = childView.Frame;
                    UIEdgeInsets margin = _controlWrapper.Margin;

                    if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent)
                    {
                        // Panel width will size to content
                        //
                        childFrame.X = margin.Left;
                        panelSize.Width = childFrame.X + childFrame.Width + margin.Right;
                    }
                    else
                    {
                        // Panel width is explicit, so align content using the content horizontal alignment (along with margin)
                        //
                        childFrame.X = margin.Left;

                        if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent)
                        {
                            // Child will fill parent (less margins)
                            //
                            childFrame.Width = panelSize.Width - (margin.Left + margin.Right);
                        }
                        else
                        {
                            // Align child in parent
                            //
                            if (_controlWrapper.HorizontalAlignment == HorizontalAlignment.Center)
                            {
                                // Ignoring margins on center for now.
                                childFrame.X = (panelSize.Width - childFrame.Width) / 2;
                            }
                            else if (_controlWrapper.HorizontalAlignment == HorizontalAlignment.Right)
                            {
                                childFrame.X = (panelSize.Width - childFrame.Width - margin.Right);
                            }
                        }
                    }

                    if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent)
                    {
                        // Panel height will size to content
                        //
                        childFrame.Y = margin.Top;
                        panelSize.Height = childFrame.Y + childFrame.Height + margin.Bottom;
                    }
                    else if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.Explicit)
                    {
                        // Panel height is explicit, so align content using the content vertical alignment (along with margin)
                        //
                        childFrame.Y = margin.Top;

                        if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.FillParent)
                        {
                            // Child will fill parent (less margin)
                            //
                            childFrame.Height = panelSize.Height - (margin.Top + margin.Bottom);
                        }
                        else
                        {
                            // Align child in parent
                            //
                            if (_controlWrapper.VerticalAlignment == VerticalAlignment.Center)
                            {
                                // Ignoring margins on center for now.
                                childFrame.Y = (panelSize.Height - childFrame.Height) / 2;
                            }
                            else if (_controlWrapper.VerticalAlignment == VerticalAlignment.Bottom)
                            {
                                childFrame.Y = (panelSize.Height - childFrame.Height - margin.Bottom);
                            }
                        }

                    }

                    // Update the content position
                    //
                    childView.Frame = childFrame;
                    // logger.Info("Child frame: {0}", childView.Frame);
                }


                // See if the container panel changed size
                //
                if ((this.Frame.Width != panelSize.Width) || (this.Frame.Height != panelSize.Height))
                {
                    // Resize the container panel...
                    //
                    RectangleF panelFrame = this.Frame;
                    panelFrame.Size = panelSize;
                    this.Frame = panelFrame;
                    // logger.Info("Frame size chaged to: {0}", this.Frame);

                    this.UpdateSize();
                }
            }

            base.LayoutSubviews();
        }
    }

    class TableHeaderView : TableContainerView
    {
        public TableHeaderView(UITableView tableView, iOSControlWrapper controlWrapper)
            : base(tableView, controlWrapper)
        {
            _tableView = tableView;
        }

        public override void UpdateSize()
        {
            // Apparently, The UITableView doesn't really expect its header/footer to change in size after
            // it is set.  Because ours can (due to layout changes based on binding), we have to poke the
            // table view by resetting the header/footer view, which seems get it to recognize the new size.
            //
            // Normally, when a child control changes size, it just lets its superview know that it needs to
            // update its layout, but the table view apparently doesn't work that way (for header/footer).
            //
            _tableView.TableHeaderView = this;
        }
    }

    class TableFooterView : TableContainerView
    {
        public TableFooterView(UITableView tableView, iOSControlWrapper controlWrapper)
            : base(tableView, controlWrapper)
        {
            _tableView = tableView;
        }

        public override void UpdateSize()
        {
            // See comment in TableHeaderView:UpdateSize() above...
            //
            _tableView.TableFooterView = this;
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

            processElementDimensions(controlSpec, 320, 200);
            applyFrameworkElementDefaults(table);

            if (controlSpec["header"] != null)
            {
                createControls(new JArray(controlSpec["header"]), (childControlSpec, childControlWrapper) =>
                {                    
                    table.TableHeaderView = new TableHeaderView(table, childControlWrapper);
                });
            }
             
            if (controlSpec["footer"] != null)
            {
                createControls(new JArray(controlSpec["footer"]), (childControlSpec, childControlWrapper) =>
                {
                    table.TableFooterView = new TableFooterView(table, childControlWrapper);
                });
            }

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

            // Setting this to false, then back to true below, disables all row-motion animations, which
            // also eliminates to need for the footer gymnastics we used to do below, which in turn allows 
            // the list items to stay in place as new items are added to the bottom and the footer is moved
            // down.  The row motion animations were kind of nice, but were causing way too many problems 
            // to be worth their while.
            //
            UIView.AnimationsEnabled = false;

            // We remove the footer temporarily, otherwise we experience a really bad animation effect
            // during the row animations below (the rows expand/contract very quickly, while the footer
            // slowly floats to its new location).  Looks terrible, especially when filling empty list.
            //
            /*
            UIView footer = tableView.TableFooterView;
            if (footer != null)
            {
                // Note: Setting TableFooterView to null when it's already null causes a couple of very
                //       ugly issues (including a malloc error for writing to space that's already been
                //       freed, and an animation error on EndUpdates complaining about the number of
                //       rows after modification not being correct, even though they are).  So we check
                //       to make sure it's non-null before we clear it.  This is probably Xamarin.
                //
                tableView.TableFooterView = null;
            }
            */

            // Note: The UITableViewRowAnimation specified variously below control the animation of
            //       the reveal of the row itself, and are not related to the other area of row animation,
            //       where rows themselves move around to reinforce insertion/deletion of rows.
            //
            tableView.BeginUpdates();
            if (reloadRows.Count > 0)
            {
                tableView.ReloadRows(reloadRows.ToArray(), UITableViewRowAnimation.None);
            }
            if (insertRows.Count > 0)
            {
                tableView.InsertRows(insertRows.ToArray(), UITableViewRowAnimation.None);
            }
            if (deleteRows.Count > 0)
            {
                tableView.DeleteRows(deleteRows.ToArray(), UITableViewRowAnimation.None);
            }
            tableView.EndUpdates();

            UIView.AnimationsEnabled = true;

            /*
            if (footer != null)
            {
                tableView.TableFooterView = footer;
            }
            */

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

        async void listview_ItemClicked(TableSourceItem itemClicked)
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
                        await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(item.BindingContext));
                    }
                }
            }
        }

        async void listview_SelectionChanged(TableSourceItem itemClicked)
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
                            await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(item.BindingContext));
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