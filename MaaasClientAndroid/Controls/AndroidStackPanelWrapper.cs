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

            Orientation orientation = Orientation.Horizontal;
            if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
            {
                orientation = Orientation.Vertical;
            }
            layout.Orientation = orientation;

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    layout.AddView(childControlWrapper.Control);
                });
            }
        }
    }
}