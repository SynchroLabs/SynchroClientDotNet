﻿using System;

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
using Android.Graphics;

namespace MaaasClientAndroid
{
    [Activity(Label = "MaaaS IO", Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class MaaasPageActivity : Activity
    {
        StateManager _stateManager;
        AndroidPageView _pageView;

        // http://developer.android.com/guide/topics/ui/actionbar.html
        //
        // http://android-developers.blogspot.com/2012/01/say-goodbye-to-menu-button.html
        //

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return _pageView.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            if (item.ItemId == Android.Resource.Id.Home)
            {
                return _pageView.OnCommandBarUp(item);
            }
            else if (_pageView.OnOptionsItemSelected(item))
            {
                // Page view handled the item
                return true;
            }
            else
            {
                return base.OnOptionsItemSelected(item);
            }
        }
        
        async protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            string endpoint = this.Intent.Extras.GetString("endpoint");

            AndroidDeviceMetrics deviceMetrics = new AndroidDeviceMetrics(this.WindowManager.DefaultDisplay);

            // This ScrollView will consume all available screen space by default, which is what we want...
            //
            var layout = new ScrollView(this);

            AndroidAppManager appManager = new AndroidAppManager(this);
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            // Using OkHttpNetworkHandler via ModernHttpClient component
            //
            // !!! Doesn't appear to support cookies out of the box
            //
            // HttpClient httpClient = new HttpClient(new OkHttpNetworkHandler());
            // _stateManager = new StateManager(endpoint, new TransportHttp(endPoint, httpClient));
            //
            Transport transport = new TransportHttp(endpoint);
            //Transport transport = new AndroidTransportWs(this, endpoint);

            _stateManager = new StateManager(appManager, app, transport, deviceMetrics);
            _pageView = new AndroidPageView(_stateManager, _stateManager.ViewModel, this, layout);

            _pageView.setPageTitle = title => this.ActionBar.Title = title;

            SetContentView(layout);

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
            await _stateManager.startApplication();
        }

        public override void OnBackPressed()
        {
            if (_pageView.HasBackCommand)
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

