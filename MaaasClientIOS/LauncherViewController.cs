using System;
using System.Drawing;

using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MaaasCore;
using System.Collections.Generic;

namespace MaaasClientIOS
{
    public class TableSource : UITableViewSource
    {
        public delegate void AppSelectedHandler(MaaasApp app);
        public event AppSelectedHandler AppSelectedEvent;

        protected List<MaaasApp> Items;

        string cellIdentifier = "MaaasAppTableCell";
        public TableSource(List<MaaasApp> items)
        {
            Items = items;
        }

        public override int RowsInSection(UITableView tableview, int section)
        {
            return Items.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(cellIdentifier);
            // if there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, cellIdentifier);
            }

            MaaasApp maaasApp = Items[indexPath.Row];
            cell.TextLabel.Text = maaasApp.Name + " - " + maaasApp.Description;
            cell.DetailTextLabel.Text = maaasApp.Endpoint;
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true); // normal iOS behaviour is to remove the blue highlight
            if (AppSelectedEvent != null)
            {
                AppSelectedEvent(Items[indexPath.Row]);
            }
        }
    }

    [Register("LauncherView")]
    public class LauncherView : UIView
    {
        public UITableView table;

        public LauncherView()
        {
            Initialize();
        }

        public LauncherView(RectangleF bounds) : base(bounds)
        {
            Initialize();
        }

        void Initialize()
        {
            BackgroundColor = UIColor.FromRGB(0.85f, 0.86f, 0.89f);

            UILabel label = new UILabel(new RectangleF(10, 10, 300, 50));
            label.BackgroundColor = BackgroundColor;
            label.Text = "Select a Synchro application...";
            label.Font = UIFont.BoldSystemFontOfSize(16f);
            label.SizeToFit();
            this.Add(label);

            float top = label.Frame.Bottom + 10;
            table = new UITableView(new RectangleF(0, top, 320, UIScreen.MainScreen.Bounds.Height - top));
            Add(table);
        }
    }

    [Register("LauncherViewController")]
    public class LauncherViewController : UIViewController
    {
        MaaasAppManager _maaasAppManager;
        List<MaaasApp> tableItems = new List<MaaasApp>();

        LauncherView _view;

        public LauncherViewController(MaaasAppManager maaasAppManager)
        {
            this.Title = "Synchro";
            _maaasAppManager = maaasAppManager;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (iOSUtil.IsiOS7)
            {
                this.EdgesForExtendedLayout = UIRectEdge.None;
            }

            _view = new LauncherView();
            View = _view;

            TableSource source = new TableSource(tableItems);
            source.AppSelectedEvent += source_AppSelectedEvent;
            _view.table.Source = source;

            // Here is the "Add" toolbar button...
            UIBarButtonSystemItem item = (UIBarButtonSystemItem)typeof(UIBarButtonSystemItem).GetField("Add").GetValue(null);
            UIBarButtonItem addButton = new UIBarButtonItem(item, (s, e) => 
            {
                Util.debug("Launcher Add button pushed");
                AppDetailViewController detailView = new AppDetailViewController(_maaasAppManager, null);
                this.NavigationController.PushViewController(detailView, false);
            });
            NavigationItem.RightBarButtonItem = addButton;
        }

        public override void ViewWillAppear(bool animated)
        {
            tableItems.Clear();
            foreach (MaaasApp app in _maaasAppManager.Apps)
            {
                tableItems.Add(app);
            }
            _view.table.ReloadData();

            base.ViewWillAppear(animated);
        }

        void source_AppSelectedEvent(MaaasApp app)
        {
            AppDetailViewController view = new AppDetailViewController(_maaasAppManager, app);
            this.NavigationController.PushViewController(view, false);
        }
    }
}