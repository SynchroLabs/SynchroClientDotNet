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
    class AndroidPickerWrapper : AndroidControlWrapper
    {
        public AndroidPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");
            Spinner picker = new Spinner(((AndroidControlWrapper)parent).Control.Context);
            this._control = picker;

            applyFrameworkElementDefaults(picker);

            // !!!
        }
    }
}