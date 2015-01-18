using System;
using System.Drawing;

using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using SynchroCore;
using System.Collections.Generic;

namespace MaaasClientIOS
{
    public class AppTableSource : UITableViewSource
    {
        static Logger logger = Logger.GetLogger("AppTableSource");

        static string cellIdentifier = "SynchroAppTableCell";

        UINavigationController _navigationController;
        MaaasAppManager _appManager;

        public AppTableSource(UINavigationController navigationController, MaaasAppManager appManager)
        {
            _navigationController = navigationController;
            _appManager = appManager;
        }

        public override int RowsInSection(UITableView tableview, int section)
        {
            return _appManager.Apps.Count;
        }

        public override UITableViewCell GetCell(UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(cellIdentifier);
            // if there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, cellIdentifier);
            }

            MaaasApp maaasApp = _appManager.Apps[indexPath.Row];
            cell.TextLabel.Text = maaasApp.Name + " - " + maaasApp.Description;
            cell.DetailTextLabel.Text = maaasApp.Endpoint;
            cell.Accessory = UITableViewCellAccessory.DetailDisclosureButton;

            return cell;
        }

        public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
        {
            return true;
        }

        public void AddClicked()
        {
            logger.Info("Add clicked...");
            AppDetailViewController detailView = new AppDetailViewController(_appManager, null);
            _navigationController.PushViewController(detailView, false);
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            logger.Info("Item selected at row #" + indexPath.Row);
            
            tableView.DeselectRow(indexPath, true); // normal iOS behaviour is to remove the blue highlight

            MaaasApp app = _appManager.Apps[indexPath.Row];

            logger.Info("Launching page at enpoint: " + app.Endpoint);
            MaaasPageViewController view = new MaaasPageViewController(_appManager, app);

            // Hide the nav controller, since the Synchro page view has its own...
            _navigationController.SetNavigationBarHidden(true, false);
            _navigationController.PushViewController(view, false);
        }

        public override void AccessoryButtonTapped(UITableView tableView, NSIndexPath indexPath)
        {
            logger.Info("Disclosure button tapped for row #" + indexPath.Row);

            MaaasApp app = _appManager.Apps[indexPath.Row];
            AppDetailViewController view = new AppDetailViewController(_appManager, app);

            _navigationController.PushViewController(view, false);
        }

        public async override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
        {
            if (editingStyle == UITableViewCellEditingStyle.Delete)
            {
                logger.Info("Item deleted at row #" + indexPath.Row);

                MaaasApp app = _appManager.Apps[indexPath.Row];
                _appManager.Apps.Remove(app);
                await _appManager.saveState();
                tableView.DeleteRows(new NSIndexPath[1]{indexPath}, UITableViewRowAnimation.Automatic);
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
        static Logger logger = Logger.GetLogger("LaunchViewController");

        MaaasAppManager _appManager;
        AppTableSource _source;

        public LauncherViewController(MaaasAppManager appManager)
        {
            this.Title = "Synchro";
            _appManager = appManager;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (iOSUtil.IsiOS7)
            {
                this.EdgesForExtendedLayout = UIRectEdge.None;
            }

            View = new LauncherView();

            _source = new AppTableSource(this.NavigationController, _appManager);
            ((LauncherView)View).table.Source = _source;

            // Here is the "Add" toolbar button...
            UIBarButtonSystemItem item = (UIBarButtonSystemItem)typeof(UIBarButtonSystemItem).GetField("Add").GetValue(null);
            UIBarButtonItem addButton = new UIBarButtonItem(item, (s, e) => 
            {
                logger.Debug("Launcher Add button pushed");
                _source.AddClicked();                
            });
            NavigationItem.RightBarButtonItem = addButton;
        }

        public override void ViewWillAppear(bool animated)
        {
            // We hide this when navigating to app, so we need to show it here in case we're navigating
            // back from the app.
            //
            this.NavigationController.SetNavigationBarHidden(false, false);

            base.ViewWillAppear(animated);
        }
    }
}