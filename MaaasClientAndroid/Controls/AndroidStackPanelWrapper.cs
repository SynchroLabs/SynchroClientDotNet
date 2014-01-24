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

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(this.Control));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    LinearLayout.LayoutParams layoutParams = null;

                    if (childControlWrapper.Control.LayoutParameters != null)
                    {
                        if (childControlWrapper.Control.LayoutParameters is ViewGroup.MarginLayoutParams)
                        {
                            layoutParams = new LinearLayout.LayoutParams((ViewGroup.MarginLayoutParams)childControlWrapper.Control.LayoutParameters);
                        }
                        else
                        {
                            layoutParams = new LinearLayout.LayoutParams(childControlWrapper.Control.LayoutParameters);
                        }
                    }
                    else
                    {
                        layoutParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                    }

                    // In order to align each item independantly, just set the Gravity for the item...
                    //
                    // layoutParams.Gravity = GravityFlags.Bottom;
                    //

                    childControlWrapper.Control.LayoutParameters = layoutParams;

                    layout.AddView(childControlWrapper.Control);
                });
            }

            layout.ForceLayout();
        }
    }
}