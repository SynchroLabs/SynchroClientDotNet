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
using Android.Graphics;

namespace MaaasClientAndroid
{
    [Activity(Label = "MaaaS IO", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class MainActivity : Activity
    {
        static string _host = Util.getMaaasHost();

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

            AndroidDeviceMetrics deviceMetrics = new AndroidDeviceMetrics(this.WindowManager.DefaultDisplay);

            // This ScrollView will consume all available screen space by default, which is what we want...
            //
            var layout = new ScrollView(this);
            layout.ChildViewAdded += layout_ChildViewAdded;

            // Using OkHttpNetworkHandler via ModernHttpClient component
            //
            // !!! Doesn't appear to support cookies out of the box
            //
            // HttpClient httpClient = new HttpClient(new OkHttpNetworkHandler());
            // _stateManager = new StateManager(_host, new TransportHttp(httpClient, _host + "/api"));
            //
            Transport transport = new TransportHttp(_host + "/api");
            //Transport transport = new AndroidTransportWs(this, _host + "/api");

            _stateManager = new StateManager(_host, transport, deviceMetrics);
            _pageView = new AndroidPageView(_stateManager, _stateManager.ViewModel, this, layout);

            _stateManager.Path = "menu";

            _pageView.setPageTitle = title => this.ActionBar.Title = title;

            SetContentView(layout);

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
            await _stateManager.loadLayout();
        }

        // When we add a child view to a ScrollView and that child has a variable size in the dimension
        // of the scroll, the MatchParent does not actually cause the child to fill the scroll area.
        // Instead, we have to set the FillViewport property on the ScrollView.  This will cause the
        // child to be at least as large as the scroll content area in the dimension of the scroll, 
        // but if the child is larger, it will work fine (it will actually scroll).
        //
        // Since the main page scrollbar is vertical, we only need to deal with height here (MatchParent
        // perpendicular to the direction of scrolling works just fine without our help)
        //
        // We do this here because this main page ScrollView is re-used, so we need to set the FillViewport
        // every time a child is added (there should only ever be one child at a time).
        //
        void layout_ChildViewAdded(object sender, ViewGroup.ChildViewAddedEventArgs e)
        {
            ScrollView scrollView = (ScrollView)sender;
            if (e.Child.LayoutParameters.Height == ViewGroup.LayoutParams.MatchParent)
            {
                scrollView.FillViewport = true;
            }
            else
            {
                scrollView.FillViewport = false;
            }
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

