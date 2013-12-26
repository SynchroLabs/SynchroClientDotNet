using System;
using MonoTouch.UIKit;
using System.Drawing;
using MaaasCore;
using System.Net.Http;
using MaaasShared;

namespace MaaasClientIOS
{
    public class MyViewController : UIViewController
    {
        static string _host = "192.168.1.109:1337"; // "localhost:1337";
        //static string _host = "maaas.azurewebsites.net";

        StateManager _stateManager;
        PageView _pageView;

        public MyViewController()
        {
        }

        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _stateManager = new StateManager(_host, new TransportHttp(_host + "/api"));
            _pageView = new PageView(_stateManager, _stateManager.ViewModel);

            View.Frame = UIScreen.MainScreen.Bounds;
            View.BackgroundColor = UIColor.White;
            View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            _stateManager.Path = "menu";

            _pageView.setPageTitle = title => Util.debug("!!! I should probably set the title to: " + title);
            _pageView.setBackEnabled = isEnabled => Util.debug("!!! I should probably remember that back enabled got set to: " + isEnabled);

            _pageView.Content = View;

            _stateManager.SetProcessingHandlers(json => _pageView.processPageView(json), json => _pageView.processMessageBox(json));
            await _stateManager.loadLayout();
        }
    }
}
