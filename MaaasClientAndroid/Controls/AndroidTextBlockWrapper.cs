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
    class AndroidTextBlockWrapper : AndroidControlWrapper
    {
        public AndroidTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text view element with text of: " + controlSpec["value"]);

            TextView textView = new TextView(((AndroidControlWrapper)parent).Control.Context);
            this._control = textView;

            applyFrameworkElementDefaults(textView);

            processElementProperty((string)controlSpec["value"], value => textView.Text = ToString(value));
        }
    }
}