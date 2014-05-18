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
        MaaasApp _maaasApp;

        StateManager _stateManager;
        PageView _pageView;

        public MaaasPageViewController(MaaasApp maaasApp)
        {
            _maaasApp = maaasApp;
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Frame = UIScreen.MainScreen.Bounds;
            View.BackgroundColor = UIColor.White;
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            MaaasDeviceMetrics deviceMetrics = new iOSDeviceMetrics();

            // Using AFNetworkHandler via ModernHttpClient component
            HttpClient httpClient = new HttpClient(new AFNetworkHandler());
            Transport transport = new TransportHttp(_maaasApp.Endpoint, httpClient);
            //Transport transport = new iOSTransportWs(this, _maaasApp.Endpoint);

            _stateManager = new StateManager(_maaasApp.Endpoint, transport, deviceMetrics);
            _pageView = new iOSPageView(_stateManager, _stateManager.ViewModel, View);

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
            await _stateManager.startApplication();
        }
    }
}
