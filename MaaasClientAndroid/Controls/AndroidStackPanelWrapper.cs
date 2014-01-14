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
        public GravityFlags ToHorizontalAlignment(object value, GravityFlags defaultAlignment = GravityFlags.Left)
        {
            GravityFlags alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Left")
            {
                alignment = GravityFlags.Left;
            }
            if (alignmentValue == "Right")
            {
                alignment = GravityFlags.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.Center;
            }
            return alignment;
        }

        public GravityFlags ToVerticalAlignment(object value, GravityFlags defaultAlignment = GravityFlags.Top)
        {
            GravityFlags alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Top")
            {
                alignment = GravityFlags.Top;
            }
            if (alignmentValue == "Right")
            {
                alignment = GravityFlags.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.Center;
            }
            return alignment;
        }

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

            if (orientation == Orientation.Vertical)
            {
                layout.SetHorizontalGravity(ToHorizontalAlignment(controlSpec["alignContent"]));
            }
            else
            {
                layout.SetVerticalGravity(ToHorizontalAlignment(controlSpec["alignContent"]));
            }

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