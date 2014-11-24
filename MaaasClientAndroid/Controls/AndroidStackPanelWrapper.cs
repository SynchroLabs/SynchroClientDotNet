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

namespace SynchroClientAndroid.Controls
{
    class AndroidStackPanelWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidStackPanelWrapper");

        public AndroidStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating stack panel element");

            LinearLayout layout = new LinearLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = layout;

            applyFrameworkElementDefaults(layout);

            // When orientation is horizontal, items are baseline aligned by default, and in this case all the vertical gravity does is specify
            // how to position the entire group of baseline aligned items if there is extra vertical space.  This is not what we want.  Turning
            // off baseline alignment causes the vertical gravity to work as expected (aligning each item to top/center/bottom).
            //
            layout.BaselineAligned = false;

            processElementProperty(controlSpec["orientation"], value => layout.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(this.Control));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    childControlWrapper.AddToLinearLayout(layout, childControlSpec);
                });
            }

            layout.ForceLayout();
        }
    }
}