using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace SynchroClientAndroid.Controls
{
    class AndroidScrollWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidScrollWrapper");

        public AndroidScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating scroll element");

            FrameLayout scroller = null;

            // ScrollView scrolls vertically only.  For horizontal use HorizontalScrollView
            //
            // http://developer.android.com/reference/android/widget/ScrollView.html
            //
            // Vertical scroll is default...
            //
            Orientation orientation = ToOrientation((string)controlSpec["orientation"], Orientation.Vertical);
            if (orientation == Orientation.Vertical)
            {
                ScrollView vScroller = new ScrollView(((AndroidControlWrapper)parent).Control.Context);
                scroller = vScroller;
            }
            else
            {
                HorizontalScrollView hScroller = new HorizontalScrollView(((AndroidControlWrapper)parent).Control.Context);
                scroller = hScroller;
            }

            scroller.ChildViewAdded += scroller_ChildViewAdded;

            _control = scroller;
            _control.OverScrollMode = OverScrollMode.Never;

            applyFrameworkElementDefaults(_control);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    scroller.AddView(childControlWrapper.Control);
                });
            }
        }


        // When we add a child view to a ScrollView and that child has a variable size in the dimension
        // of the scroll, the MatchParent does not actually cause the child to fill the scroll area.
        // Instead, we have to set the FillViewport property on the ScrollView.  This will cause the
        // child to be at least as large as the scroll content area in the dimension of the scroll, 
        // but if the child is larger, it will work fine (it will actually scroll).
        //
        // Variable ("star" or MatchParent) sizing perpendicular to the direction of scroll works just
        // fine without our help.
        //
        void scroller_ChildViewAdded(object sender, ViewGroup.ChildViewAddedEventArgs e)
        {
            ScrollView scrollView = sender as ScrollView;
            if (scrollView != null)
            {
                if (e.Child.LayoutParameters.Height == ViewGroup.LayoutParams.MatchParent)
                {
                    scrollView.FillViewport = true;
                }
                else
                {
                    scrollView.FillViewport = false;
                }
            }

            HorizontalScrollView hScrollView = sender as HorizontalScrollView;
            if (hScrollView != null)
            {
                if (e.Child.LayoutParameters.Width == ViewGroup.LayoutParams.MatchParent)
                {
                    hScrollView.FillViewport = true;
                }
                else
                {
                    hScrollView.FillViewport = false;
                }
            }
        }
    }
}