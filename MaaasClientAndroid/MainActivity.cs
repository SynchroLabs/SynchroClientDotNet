using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using MaaasCore;
using MaaasShared;
using System.Net.Http;
using ModernHttpClient;
using Android.Util;

namespace MaaasClientAndroid
{
    [Activity(Label = "Loading...", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class MainActivity : Activity
    {
        static string _host = "192.168.1.109:1337"; // "localhost:1337";
        //static string _host = "maaas.azurewebsites.net";

        StateManager _stateManager;
        PageView _pageView;

        bool _isAppBackEnabled = false;

        async protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            AndroidDeviceMetrics deviceMetrics = new AndroidDeviceMetrics(this.WindowManager.DefaultDisplay);

            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;

            // Using OkHttpNetworkHandler via ModernHttpClient component
            //
            // !!! Doesn't appear to support cookies out of the box
            //
            // HttpClient httpClient = new HttpClient(new OkHttpNetworkHandler());
            // _stateManager = new StateManager(_host, new TransportHttp(httpClient, _host + "/api"));
            //
            _stateManager = new StateManager(_host, new TransportHttp(_host + "/api"), deviceMetrics);
            _pageView = new AndroidPageView(_stateManager, _stateManager.ViewModel, this, layout);

            _stateManager.Path = "menu";

            _pageView.setPageTitle = title => this.ActionBar.Title = title;
            _pageView.setBackEnabled = isEnabled => _isAppBackEnabled = isEnabled;

            SetContentView(layout);

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
            await _stateManager.loadLayout();
        }

        public override void OnBackPressed()
        {
            if (_isAppBackEnabled)
            {
                _pageView.OnBackCommand();
            }
            else
            {
                this.Finish();
            }
        }
    }
}

