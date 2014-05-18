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

        List<MaaasApp> tableItems;

        string cellIdentifier = "MaaasAppTableCell";
        public TableSource(List<MaaasApp> items)
        {
            tableItems = items;
        }

        public override int RowsInSection(UITableView tableview, int section)
        {
            return tableItems.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(cellIdentifier);
            // if there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, cellIdentifier);
            }

            MaaasApp maaasApp = tableItems[indexPath.Row];
            cell.TextLabel.Text = maaasApp.Name + " - " + maaasApp.Description;
            cell.DetailTextLabel.Text = maaasApp.Endpoint;
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true); // normal iOS behaviour is to remove the blue highlight
            if (AppSelectedEvent != null)
            {
                AppSelectedEvent(tableItems[indexPath.Row]);
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
            UILabel label = new UILabel(new RectangleF(10, 10, 300, 50));
            label.Text = "Select an app...";
            label.SizeToFit();
            this.Add(label);

            table = new UITableView(new RectangleF(10, 70, 300, 300));
            Add(table);

            BackgroundColor = UIColor.Red;
        }
    }

    [Register("LauncherViewController")]
    public class LauncherViewController : UIViewController
    {
        MaaasAppManager _maaasAppManager;
        List<MaaasApp> tableItems = new List<MaaasApp>();

        public LauncherViewController(MaaasAppManager maaasAppManager)
        {
            this.Title = "MaaaS";
            _maaasAppManager = maaasAppManager;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            LauncherView view = new LauncherView();
            View = view;

            foreach (MaaasApp app in _maaasAppManager.Apps)
            {
               tableItems.Add(app);
            }

            TableSource source = new TableSource(tableItems);
            source.AppSelectedEvent += source_AppSelectedEvent;
            view.table.Source = source;
        }

        void source_AppSelectedEvent(MaaasApp app)
        {
            MaaasPageViewController view = new MaaasPageViewController(app);
            this.NavigationController.SetNavigationBarHidden(true, false);
            this.NavigationController.PushViewController(view, false);
        }
    }
}