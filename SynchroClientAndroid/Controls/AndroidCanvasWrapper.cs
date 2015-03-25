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
using SynchroCore;

namespace SynchroClientAndroid.Controls
{
    class AndroidCanvasWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidCanvasWrapper");

        public AndroidCanvasWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating canvas element");

            AbsoluteLayout absLayout = new AbsoluteLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = absLayout;

            // !!! Absolute layout supports padding
            // !!! http://alvinalexander.com/java/jwarehouse/android/core/java/android/widget/AbsoluteLayout.java.shtml

            applyFrameworkElementDefaults(absLayout);
            processElementProperty(controlSpec["background"], value => absLayout.SetBackgroundColor(ToColor(value)));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // Create an appropriate set of LayoutParameters...
                    //
                    if (childControlWrapper.Control.LayoutParameters != null)
                    {
                        childControlWrapper.Control.LayoutParameters = new AbsoluteLayout.LayoutParams(
                            childControlWrapper.Control.LayoutParameters.Width, childControlWrapper.Control.LayoutParameters.Height, 0, 0
                            );
                    }
                    else
                    {
                        childControlWrapper.Control.LayoutParameters = new AbsoluteLayout.LayoutParams(
                            AbsoluteLayout.LayoutParams.WrapContent, AbsoluteLayout.LayoutParams.WrapContent, 0, 0
                            );
                    }

                    absLayout.AddView(childControlWrapper.Control);

                    // Bind the x and y position to the appropriate properties of the AbsoluteLayout.LayoutParams....
                    //
                    childControlWrapper.processElementProperty(childControlSpec["left"], value =>
                    {
                        ((AbsoluteLayout.LayoutParams)childControlWrapper.Control.LayoutParameters).X = (int)ToDeviceUnits(value);
                        absLayout.ForceLayout();
                    });
                    childControlWrapper.processElementProperty(childControlSpec["top"], value => {
                        ((AbsoluteLayout.LayoutParams)childControlWrapper.Control.LayoutParameters).Y = (int)ToDeviceUnits(value);
                        absLayout.ForceLayout();
                    });
                });
            }
        }
    }
}