using System;
using MonoTouch.UIKit;
using System.Drawing;
using MaaasCore;
using System.Net.Http;
using MaaasShared;
using ModernHttpClient;

namespace MaaasClientIOS
{
    public class MaaasPageViewController : UIViewController
    {
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
            Transport transport = new TransportHttp(_maaasApp.Endpoint, httpClient);
            //Transport transport = new iOSTransportWs(this, _maaasApp.Endpoint);

            _stateManager = new StateManager(_appManager, _maaasApp, transport, deviceMetrics);
            _pageView = new iOSPageView(_stateManager, _stateManager.ViewModel, View);

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView, _pageView.ProcessMessageBox);
            await _stateManager.startApplication();
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

        // Is called when the OS is going to rotate the application. It handles rotating the status bar
        // if it's present, as well as it's controls like the navigation controller and tab bar, but you 
        // must handle the rotation of your view and associated subviews. This call is wrapped in an 
        // animation block in the underlying implementation, so it will automatically animate your control
        // repositioning.
        //
        public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            // this.InterfaceOrientation == UIInterfaceOrientation.
            base.WillAnimateRotation(toInterfaceOrientation, duration);

            // !!! Do our own rotation handling here
            if (normalizeOrientation(toInterfaceOrientation) == UIInterfaceOrientation.Portrait)
            {
                Util.debug("Screen oriented to Portrait");
                _stateManager.processViewUpdate(MaaasOrientation.Portrait);
            }
            else 
            {
                Util.debug("Screen oriented to Landscape");
                _stateManager.processViewUpdate(MaaasOrientation.Landscape);
            }

            ((iOSPageView)_pageView).UpdateLayout();
        }

    }
}
