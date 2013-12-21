using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using MaaasCore;

namespace MaaasClientAndroid
{
    [Activity(Label = "Loading...", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        static string _host = "192.168.1.109:1337"; // "localhost:1337";
        //static string _host = "maaas.azurewebsites.net";

        StateManager _stateManager;
        PageView _pageView;

        bool _isAppBackEnabled = false;

        protected override void OnCreate(Bundle bundle)
        {
            _stateManager = new StateManager(_host);
            _pageView = new PageView(_stateManager, _stateManager.ViewModel);

            base.OnCreate(bundle);

            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;

            _stateManager.Path = "menu";

            _pageView.setPageTitle = title => this.ActionBar.Title = title;
            _pageView.setBackEnabled = isEnabled => _isAppBackEnabled = isEnabled;

            _pageView.Content = layout;

            _stateManager.SetProcessingHandlers(json => _pageView.processPageView(json), json => _pageView.processMessageBox(json));
            _stateManager.loadLayout();

            SetContentView(layout);
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

