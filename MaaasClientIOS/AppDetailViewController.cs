using System;
using System.Drawing;

using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MaaasCore;
using MaaasShared;
using Newtonsoft.Json.Linq;

namespace MaaasClientIOS
{
    public enum DisplayMode { Find, Add, View };

    [Register("AppDetailView")]
    public class AppDetailView : UIView
    {
        public delegate void AppFindHandler(string endpoint);
        public delegate void AppSaveHandler();
        public delegate void AppLaunchHandler();
        public delegate void AppDeleteHandler();

        public event AppFindHandler AppFindEvent;
        public event AppSaveHandler AppSaveEvent;
        public event AppLaunchHandler AppLaunchEvent;
        public event AppDeleteHandler AppDeleteEvent;

        protected UILabel capEndpoint;
        protected UITextField editEndpoint;
        protected UIButton btnFind;
        protected UILabel valEndpoint;
        protected UILabel capName;
        protected UILabel valName;
        protected UILabel capDesc;
        protected UILabel valDesc;
        protected UIButton btnSave;
        protected UIButton btnLaunch;
        protected UIButton btnDelete;

        public AppDetailView(MaaasApp app)
        {
            Initialize();
            if (app != null)
            {
                this.Populate(app);
                UpdateVisibility(DisplayMode.View);
            }
            else
            {
                UpdateVisibility(DisplayMode.Find);
            }
        }

        public void UpdateVisibility(DisplayMode mode)
        {
            editEndpoint.Hidden = mode != DisplayMode.Find;
            btnFind.Hidden = mode != DisplayMode.Find;

            valEndpoint.Hidden = mode == DisplayMode.Find;
            capName.Hidden = mode == DisplayMode.Find;
            valName.Hidden = mode == DisplayMode.Find;
            capDesc.Hidden = mode == DisplayMode.Find;
            valDesc.Hidden = mode == DisplayMode.Find;

            btnSave.Hidden = mode != DisplayMode.Add;
            btnLaunch.Hidden = mode != DisplayMode.View;
            btnDelete.Hidden = mode != DisplayMode.View;
        }

        public void Populate(MaaasApp app)
        {
            valEndpoint.Text = app.Endpoint;
            valName.Text = app.Name;
            valDesc.Text = app.Description;
        }

        void Initialize()
        {
            BackgroundColor = UIColor.White;

            UIFont capFont = UIFont.BoldSystemFontOfSize(16f);
            UIColor capColor = UIColor.Black;

            UIFont valFont = UIFont.SystemFontOfSize(16f);
            UIColor valColor = UIColor.DarkGray;

            float spacing = 10;

            float x = 10;
            float leftMargin = 10;
            float maxWidth = UIScreen.MainScreen.Bounds.Width - (2 * leftMargin);

            capEndpoint = new UILabel(new RectangleF(leftMargin, x, maxWidth, 50));
            capEndpoint.Text = "Endpoint";
            capEndpoint.Font = capFont;
            capEndpoint.TextColor = capColor;
            capEndpoint.SizeToFit();
            this.Add(capEndpoint);

            x = capEndpoint.Frame.Bottom + spacing;
            
            editEndpoint = new UITextField();
            editEndpoint.BorderStyle = UITextBorderStyle.RoundedRect;
            editEndpoint.SizeToFit();
            editEndpoint.Frame = new RectangleF(leftMargin, x, maxWidth, editEndpoint.Frame.Height);
            editEndpoint.ShouldReturn += (textfield) =>
            {
                btnFind_TouchUpInside(btnFind, null);
                return true;
            };
            this.Add(editEndpoint);

            x = editEndpoint.Frame.Bottom + spacing;

            btnFind = UIButton.FromType(UIButtonType.RoundedRect);
            btnFind.Frame = new RectangleF(leftMargin, x, 100, 40);
            btnFind.SetTitle("Find", UIControlState.Normal);
            btnFind.TouchUpInside += btnFind_TouchUpInside;
            this.Add(btnFind);

            x = capEndpoint.Frame.Bottom + spacing - 5;

            valEndpoint = new UILabel();
            valEndpoint.Text = "localhost:1337/api"; // Sample text
            valEndpoint.Font = valFont;
            valEndpoint.TextColor = valColor;
            valEndpoint.SizeToFit();
            valEndpoint.Frame = new RectangleF(leftMargin, x, maxWidth, valEndpoint.Frame.Height);
            valEndpoint.Text = "";
            this.Add(valEndpoint);

            x = valEndpoint.Frame.Bottom + spacing;

            capName = new UILabel(new RectangleF(leftMargin, x, maxWidth, 50));
            capName.Text = "Name";
            capName.Font = capFont;
            capName.TextColor = capColor;
            capName.SizeToFit();
            this.Add(capName);

            x = capName.Frame.Bottom + spacing - 5;

            valName = new UILabel();
            valName.Text = "synchro-samples"; // Sample text
            valName.Font = valFont;
            valName.TextColor = valColor;
            valName.SizeToFit();
            valName.Frame = new RectangleF(leftMargin, x, maxWidth, valName.Frame.Height);
            valName.Text = "";
            this.Add(valName);

            x = valName.Frame.Bottom + spacing;

            capDesc = new UILabel(new RectangleF(leftMargin, x, maxWidth, 50));
            capDesc.Text = "Description";
            capDesc.Font = capFont;
            capDesc.TextColor = capColor;
            capDesc.SizeToFit();
            this.Add(capDesc);

            x = capDesc.Frame.Bottom + spacing - 5;

            valDesc = new UILabel();
            valDesc.Text = "Synchro API Samples (local)"; // Sample text
            valDesc.Font = valFont;
            valDesc.TextColor = valColor;
            valDesc.SizeToFit();
            valDesc.Frame = new RectangleF(leftMargin, x, maxWidth, valEndpoint.Frame.Height);
            valDesc.Text = "";
            this.Add(valDesc);

            x = valDesc.Frame.Bottom + spacing;

            btnSave = UIButton.FromType(UIButtonType.RoundedRect);
            btnSave.Frame = new RectangleF(leftMargin, x, 100, 40);
            btnSave.SetTitle("Save", UIControlState.Normal);
            btnSave.TouchUpInside += btnSave_TouchUpInside;
            this.Add(btnSave);

            btnLaunch = UIButton.FromType(UIButtonType.RoundedRect);
            btnLaunch.Frame = new RectangleF(leftMargin, x, 100, 40);
            btnLaunch.SetTitle("Launch", UIControlState.Normal);
            btnLaunch.TouchUpInside += btnLaunch_TouchUpInside;
            this.Add(btnLaunch);

            btnDelete = UIButton.FromType(UIButtonType.RoundedRect);
            btnDelete.Frame = new RectangleF(btnLaunch.Frame.Right + spacing, x, 100, 40);
            btnDelete.SetTitle("Delete", UIControlState.Normal);
            btnDelete.TouchUpInside += btnDelete_TouchUpInside;
            this.Add(btnDelete);
        }

        void btnFind_TouchUpInside(object sender, EventArgs e)
        {
            this.EndEditing(true);
            if (AppFindEvent != null)
            {
                AppFindEvent(this.editEndpoint.Text);
            }
        }

        void btnSave_TouchUpInside(object sender, EventArgs e)
        {
            if (AppSaveEvent != null)
            {
                AppSaveEvent();
            }
        }

        void btnLaunch_TouchUpInside(object sender, EventArgs e)
        {
            if (AppLaunchEvent != null)
            {
                AppLaunchEvent();
            }
        }

        void btnDelete_TouchUpInside(object sender, EventArgs e)
        {
            UIAlertView alertView = new UIAlertView();

            alertView.Title = "Synchro Application Delete";
            alertView.Message = "Are you sure you want to remove this Synchro application from your list";

            int idYes = alertView.AddButton("Yes");
            alertView.AddButton("No");

            alertView.Clicked += (s, b) =>
            {
                if (b.ButtonIndex == idYes)
                {
                    if (AppDeleteEvent != null)
                    {
                        AppDeleteEvent();
                    }
                }
            };

            alertView.Show();
        }
    }

    [Register("AppDetailViewController")]
    public class AppDetailViewController : UIViewController
    {
        MaaasAppManager _appManager;
        MaaasApp _app;

        AppDetailView _view;

        public AppDetailViewController(MaaasAppManager maaasAppManager, MaaasApp maaasApp)
        {
            this.Title = "Application";
            _appManager = maaasAppManager;
            _app = maaasApp;
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewWillAppear(bool animated)
        {
            // We hide this when navigating to app, so we need to show it here in case we're navigating
            // back from the app.
            //
            this.NavigationController.SetNavigationBarHidden(false, false);
        }

        public override void ViewDidLoad()
        {
            if (iOSUtil.IsiOS7)
            {
                this.EdgesForExtendedLayout = UIRectEdge.None;
            }

            _view = new AppDetailView(_app);
            View = _view;

            var tap = new UITapGestureRecognizer { CancelsTouchesInView = false };
            tap.AddTarget(() => View.EndEditing(true));
            tap.ShouldReceiveTouch += (recognizer, touch) => !(touch.View is UIButton);
            View.AddGestureRecognizer(tap);

            _view.AppFindEvent += view_AppFindEvent;
            _view.AppSaveEvent += view_AppSaveEvent;
            _view.AppLaunchEvent += view_AppLaunchEvent;
            _view.AppDeleteEvent += view_AppDeleteEvent;

            base.ViewDidLoad();
        }

        async void view_AppFindEvent(string endpoint)
        {
            var managedApp = _appManager.GetApp(endpoint);
            if (managedApp != null)
            {
                UIAlertView alertView = new UIAlertView();
                alertView.Title = "Synchro Application Search";
                alertView.Message = "You already have a Synchro application with the supplied endpoint in your list";
                alertView.AddButton("OK");
                alertView.Show();
                return;
            }

            bool formatException = false;
            try
            {
                Uri endpointUri = TransportHttp.UriFromHostString(endpoint);
                Transport transport = new TransportHttp(endpointUri);

                JObject appDefinition = await transport.getAppDefinition();
                if (appDefinition == null)
                {
                    UIAlertView alertView = new UIAlertView();
                    alertView.Title = "Synchro Application Search";
                    alertView.Message = "No Synchro application found at the supplied endpoint";
                    alertView.AddButton("OK");
                    alertView.Show();
                    return;
                }
                else
                {
                    _app = new MaaasApp(endpoint, appDefinition);
                    _view.Populate(_app);
                    _view.UpdateVisibility(DisplayMode.Add);
                }
            }
            catch (FormatException)
            {
                // Can't await async message dialog in catch block (until C# 6.0).
                //
                formatException = true;
            }

            if (formatException)
            {
                UIAlertView alertView = new UIAlertView();
                alertView.Title = "Synchro Application Search";
                alertView.Message = "Endpoint not formatted correctly";
                alertView.AddButton("OK");
                alertView.Show();
            }
        }

        async void view_AppSaveEvent()
        {
            _appManager.Apps.Add(_app);
            await _appManager.saveState();
            this.NavigationController.PopViewControllerAnimated(true);
        }

        void view_AppLaunchEvent()
        {
            MaaasPageViewController view = new MaaasPageViewController(_appManager, _app);
            this.NavigationController.SetNavigationBarHidden(true, false);
            this.NavigationController.PushViewController(view, false);
        }

        async void view_AppDeleteEvent()
        {
            _appManager.Apps.Remove(_app);
            await _appManager.saveState();
            this.NavigationController.PopViewControllerAnimated(true);
        }
    }
}