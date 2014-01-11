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

namespace MaaasClientAndroid.Controls
{
    class AndroidScrollWrapper : AndroidControlWrapper
    {
        public AndroidScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating scroll element");

            FrameLayout scroller = null;

            // ScrollView scrolls vertically only.  For horizontal use HorizontalScrollView
            //
            // http://developer.android.com/reference/android/widget/ScrollView.html
            //
            // Vertical scroll is default...
            //
            if ((controlSpec["orientation"] == null) || ((string)controlSpec["orientation"] != "horizontal"))
            {
                ScrollView vScroller = new ScrollView(((AndroidControlWrapper)parent).Control.Context);
                scroller = vScroller;
            }
            else
            {
                HorizontalScrollView hScroller = new HorizontalScrollView(((AndroidControlWrapper)parent).Control.Context);
                scroller = hScroller;
            }

            _control = scroller;
            _control.OverScrollMode = OverScrollMode.Never;

            if (_control.LayoutParameters == null)
            {
                _control.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            }

            applyFrameworkElementDefaults(_control);

            processElementProperty((string)controlSpec["height"], value => _control.LayoutParameters.Height = (int)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["width"], value => _control.LayoutParameters.Width = (int)ToDeviceUnits(value));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    scroller.AddView(childControlWrapper.Control);
                });
            }
        }
    }
}