using System;
using MonoTouch.UIKit;
using System.Drawing;
using MaaasCore;
using System.Net.Http;
using MaaasShared;
using ModernHttpClient;

namespace MaaasClientIOS
{
    public class MyViewController : UIViewController
    {
        static string _host = Util.getMaaasHost();

        StateManager _stateManager;
        PageView _pageView;

        public MyViewController()
        {
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Frame = UIScreen.MainScreen.Bounds;
            View.BackgroundColor = UIColor.White;
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            MaaasDeviceMetrics deviceMetrics = new iOSDeviceMetrics();

            // Using AFNetworkHandler via ModernHttpClient component
            //HttpClient httpClient = new HttpClient(new AFNetworkHandler());
            //Transport transport = new TransportHttp(httpClient, _host + "/api");
            Transport transport = new iOSTransportWs(this, _host + "/api");

            _stateManager = new StateManager(_host, transport, deviceMetrics);
            _pageView = new iOSPageView(_stateManager, _stateManager.ViewModel, View);

            _stateManager.Path = "menu";

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
            await _stateManager.loadLayout();
        }
    }
}
