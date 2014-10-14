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
    class AndroidTextBlockWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidTextBlockWrapper");

        public AndroidTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating text view element with text of: " + controlSpec["value"]);

            TextView textView = new TextView(((AndroidControlWrapper)parent).Control.Context);
            this._control = textView;

            applyFrameworkElementDefaults(textView);

            processElementProperty((string)controlSpec["value"], value => textView.Text = ToString(value));

            processElementProperty((string)controlSpec["ellipsize"], value =>
            {
                // Other trimming options:
                //
                //   Android.Text.TextUtils.TruncateAt.Start;
                //   Android.Text.TextUtils.TruncateAt.Middle;
                //   Android.Text.TextUtils.TruncateAt.Marquee;
                //
                bool bEllipsize = ToBoolean(value);
                if (bEllipsize)
                {
                    textView.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
                }
                else
                {
                    textView.Ellipsize = null;
                }
            });

            processElementProperty((string)controlSpec["textAlignment"], value =>
            {
                // This gravity here specifies how this control's contents should be aligned within the control box, whereas
                // the layout gravity specifies how the control box itself should be aligned within its container.
                //
                String alignString = ToString(value);
                if (alignString == "Left")
                {
                    textView.Gravity = GravityFlags.Left;
                }
                if (alignString == "Center")
                {
                    textView.Gravity = GravityFlags.CenterHorizontal;
                }
                else if (alignString == "Right")
                {
                    textView.Gravity = GravityFlags.Right;
                }
            });
        }
    }
}