using System;
using MonoTouch.UIKit;
using SynchroCore;
using System.Drawing;
using System.Net.Http;
using ModernHttpClient;
using System.Threading.Tasks;

namespace MaaasClientIOS
{
    public class MaaasPageViewController : UIViewController
    {
        static Logger logger = Logger.GetLogger("MaaasPageViewController");

        MaaasAppManager _appManager;
        MaaasApp _maaasApp;

        StateManager _stateManager;
        PageView _pageView;

        public MaaasPageViewController(MaaasAppManager appManager, MaaasApp maaasApp)
        {
            _appManager = appManager;
            _maaasApp = maaasApp;
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Current orientation: this.InterfaceOrientation

            View.Frame = UIScreen.MainScreen.Bounds;
            View.BackgroundColor = UIColor.White;
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            MaaasDeviceMetrics deviceMetrics = new iOSDeviceMetrics(this);

            // Using AFNetworkHandler via ModernHttpClient component
            HttpClient httpClient = new HttpClient(new AFNetworkHandler());
            Transport transport = new TransportHttp(TransportHttp.UriFromHostString(_maaasApp.Endpoint), httpClient);
            //Transport transport = new iOSTransportWs(this, _maaasApp.Endpoint);

            Action backToMenu = null;
            if (_appManager.AppSeed == null)
            {
                // If we are't nailed to a predefined app, then we'll allow the app to navigate back to
                // this page from its top level page.
                //
                backToMenu = new Action(delegate()
                {
                    // If we are't nailed to a predefined app, then we'll allow the app to navigate back to
                    // this page from its top level page.
                    //
                    this.NavigationController.PopViewControllerAnimated(true);
                });
            }

            _stateManager = new StateManager(_appManager, _maaasApp, transport, deviceMetrics);
            _pageView = new iOSPageView(_stateManager, _stateManager.ViewModel, View, backToMenu);

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView, _pageView.ProcessMessageBox, _pageView.ProcessLaunchUrl);
            await _stateManager.startApplicationAsync();
        }

        private UIInterfaceOrientation normalizeOrientation(UIInterfaceOrientation orientation)
        {
            if (orientation == UIInterfaceOrientation.LandscapeRight)
            {
                return UIInterfaceOrientation.LandscapeLeft;
            }
            else if (orientation == UIInterfaceOrientation.PortraitUpsideDown)
            {
                return UIInterfaceOrientation.Portrait;
            }

            return orientation;
        }

        // When the device rotates, the OS calls this method to determine if it should try and rotate the
        // application and then call WillAnimateRotation
        //
        // The method that this method overrides is obsolete, which was causing a compiler warning.  Since
        // we allow rotation in all cases (at least for now), we don't need this anyway.
        /*
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            // We're passed to orientation that it will rotate to. We could just return true, but this
            // switch illustrates how you can test for the different cases.
            //
            switch (toInterfaceOrientation)
            {
                case UIInterfaceOrientation.LandscapeLeft:
                case UIInterfaceOrientation.LandscapeRight:
                case UIInterfaceOrientation.Portrait:
                case UIInterfaceOrientation.PortraitUpsideDown:
                default:
                    return true;
            }
        }
         */

        // Is called when the OS is going to rotate the application. It handles rotating the status bar
        // if it's present, as well as it's controls like the navigation controller and tab bar, but you 
        // must handle the rotation of your view and associated subviews. This call is wrapped in an 
        // animation block in the underlying implementation, so it will automatically animate your control
        // repositioning.
        //
        public override async void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            // this.InterfaceOrientation == UIInterfaceOrientation.
            base.WillAnimateRotation(toInterfaceOrientation, duration);

            // Do our own rotation handling here
            if (normalizeOrientation(toInterfaceOrientation) == UIInterfaceOrientation.Portrait)
            {
                logger.Debug("Screen oriented to Portrait");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Portrait);
            }
            else 
            {
                logger.Debug("Screen oriented to Landscape");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Landscape);
            }

            ((iOSPageView)_pageView).UpdateLayout();
        }
    }
}
