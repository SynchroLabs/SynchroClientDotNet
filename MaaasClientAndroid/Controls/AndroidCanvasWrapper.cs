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
    class AndroidCanvasWrapper : AndroidControlWrapper
    {
        public AndroidCanvasWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating canvas element");

            AbsoluteLayout absLayout = new AbsoluteLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = absLayout;

            applyFrameworkElementDefaults(absLayout);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // We need to capture and potentially bind some attributes on the added child controls here in the context of the parent...
                    //
                    // AbsoluteLayout.LayoutParams layoutParams = new AbsoluteLayout.LayoutParams(childControlWrapper.Control.Width, childControlWrapper.Control.Height, 0, 0);
                    AbsoluteLayout.LayoutParams layoutParams = new AbsoluteLayout.LayoutParams(128, 128, 0, 0);
                    absLayout.AddView(childControlWrapper.Control, layoutParams);

                    childControlWrapper.processElementProperty((string)childControlSpec["left"], value => {
                        ((AbsoluteLayout.LayoutParams)childControlWrapper.Control.LayoutParameters).X = (int)ToDouble(value);
                        absLayout.ForceLayout();
                    });
                    childControlWrapper.processElementProperty((string)childControlSpec["top"], value => {
                        ((AbsoluteLayout.LayoutParams)childControlWrapper.Control.LayoutParameters).Y = (int)ToDouble(value);
                        absLayout.ForceLayout();
                    });
                });
            }
        }
    }
}