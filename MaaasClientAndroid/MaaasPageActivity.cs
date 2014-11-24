﻿using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using MaaasCore;
using System.Net.Http;
using ModernHttpClient;
using Android.Util;
using Android.Graphics;
using Android.Content.PM;
using Android.Support.V4.App;
using System.Threading.Tasks;

namespace SynchroClientAndroid
{
    [Activity(Label = "Synchro", Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo", ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MaaasPageActivity : Activity
    {
        static Logger logger = Logger.GetLogger("MaaasPageActivity");

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

        // http://stackoverflow.com/questions/21731977/get-the-current-screen-orientation-in-monodroid
        //
        public ScreenOrientation GetScreenOrientation()
        {
            ScreenOrientation orientation;
            SurfaceOrientation rotation = WindowManager.DefaultDisplay.Rotation;

            DisplayMetrics dm = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(dm);

            if ((rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180) && dm.HeightPixels > dm.WidthPixels
                || (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270) && dm.WidthPixels > dm.HeightPixels)
            {
                // The device's natural orientation is portrait
                switch (rotation)
                {
                    case SurfaceOrientation.Rotation0:
                        orientation = ScreenOrientation.Portrait;
                        break;
                    case SurfaceOrientation.Rotation90:
                        orientation = ScreenOrientation.Landscape;
                        break;
                    case SurfaceOrientation.Rotation180:
                        orientation = ScreenOrientation.ReversePortrait;
                        break;
                    case SurfaceOrientation.Rotation270:
                        orientation = ScreenOrientation.ReverseLandscape;
                        break;
                    default:
                        orientation = ScreenOrientation.Portrait;
                        break;
                }
            }
            else
            {
                // The device's natural orientation is landscape or if the device is square
                switch (rotation)
                {
                    case SurfaceOrientation.Rotation0:
                        orientation = ScreenOrientation.Landscape;
                        break;
                    case SurfaceOrientation.Rotation90:
                        orientation = ScreenOrientation.Portrait;
                        break;
                    case SurfaceOrientation.Rotation180:
                        orientation = ScreenOrientation.ReverseLandscape;
                        break;
                    case SurfaceOrientation.Rotation270:
                        orientation = ScreenOrientation.ReversePortrait;
                        break;
                    default:
                        orientation = ScreenOrientation.Landscape;
                        break;
                }
            }

            return orientation;
        }

        async protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            string endpoint = this.Intent.Extras.GetString("endpoint");

            AndroidDeviceMetrics deviceMetrics = new AndroidDeviceMetrics(this);

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
            //
            Transport transport = new TransportHttp(TransportHttp.UriFromHostString(endpoint));

            Action backToMenu = null;
            if (appManager.AppSeed == null)
            {
                // If we are't nailed to a predefined app, then we'll allow the app to navigate back to
                // this page from its top level page.
                //
                backToMenu = new Action(delegate()
                {
                    var intent = new Intent(this, typeof(AppDetailActivity));
                    intent.PutExtra("endpoint", app.Endpoint);
                    NavUtils.NavigateUpTo(this, intent);
                });
            }

            _stateManager = new StateManager(appManager, app, transport, deviceMetrics);
            _pageView = new AndroidPageView(_stateManager, _stateManager.ViewModel, this, layout, backToMenu);

            _pageView.setPageTitle = title => this.ActionBar.Title = title;

            SetContentView(layout);

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView,_pageView.ProcessMessageBox);
            await _stateManager.startApplicationAsync();
        }

        public override async void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            if (newConfig.Orientation == Android.Content.Res.Orientation.Portrait)
            {
                // Changed to portrait
                logger.Debug("Screen oriented to Portrait");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Portrait);
            }
            else if (newConfig.Orientation == Android.Content.Res.Orientation.Landscape)
            {
                // Changed to landscape
                logger.Debug("Screen oriented to Landscape");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Landscape);
            }
        }

        public override async void OnBackPressed()
        {
            if (_pageView.HasBackCommand)
            {
                await _pageView.OnBackCommand();
            }
            else
            {
                this.Finish();
            }
        }
    }
}

