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
    class AndroidStackPanelWrapper : AndroidControlWrapper
    {
        public AndroidStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stack panel element");

            LinearLayout layout = new LinearLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = layout;

            applyFrameworkElementDefaults(layout);

            // When orientation is horizontal, items are baseline aligned by default, and in this case all the vertical gravity does is specify
            // how to position the entire group of baseline aligned items if there is extra vertical space.  This is not what we want.  Turning
            // off baseline alignment causes the vertical gravity to work as expected (aligning each item to top/center/bottom).
            //
            layout.BaselineAligned = false;

            processElementProperty((string)controlSpec["orientation"], value => layout.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

            processElementProperty((string)controlSpec["alignContentH"], value => layout.SetHorizontalGravity(ToHorizontalAlignment(value, GravityFlags.Left)), GravityFlags.Left);
            processElementProperty((string)controlSpec["alignContentV"], value => layout.SetVerticalGravity(ToVerticalAlignment(value, GravityFlags.Center)), GravityFlags.Center);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // In order to align each item independantly, get the current size from any existing LayoutParams, then using that size, create
                    // a LinearLayout.LayoutParams (there will often not be any LayoutParams in existence at this time, and even if there is, is 
                    // could be of a more generic type).  Then the gravity can be set for the item (both horizontal and vertical, or'ed together).
                    // Lastly, set the LineaeLayout.LayoutParams as the item LayoutParams.
                    //
                    // The code below has been tested and verified...
                    //
                    /*
                    int height = ViewGroup.LayoutParams.WrapContent;
                    int width = ViewGroup.LayoutParams.WrapContent;
                    if (childControlWrapper.Control.LayoutParameters != null)
                    {
                        height = childControlWrapper.Control.LayoutParameters.Height;
                        width = childControlWrapper.Control.LayoutParameters.Width;
                    }
                    LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(width, height);
                    layoutParams.Gravity = GravityFlags.Bottom;
                    childControlWrapper.Control.LayoutParameters = layoutParams;
                    */

                    layout.AddView(childControlWrapper.Control);
                });
            }

            layout.ForceLayout();
        }
    }
}