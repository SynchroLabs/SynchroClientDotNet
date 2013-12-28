﻿using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using MaaasCore;
using MaaasShared;

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

            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;

            _stateManager = new StateManager(_host, new TransportHttp(_host + "/api"));
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

