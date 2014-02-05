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
    // Android "Spinner" - http://developer.android.com/guide/topics/ui/controls/spinner.html
    //
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
            String[] values = new String[] { "One", "Two" };
            ArrayAdapter adapter = new ArrayAdapter(((AndroidControlWrapper)parent).Control.Context, Android.Resource.Layout.SimpleSpinnerItem, values);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            picker.Adapter = adapter;
        }
    }
}