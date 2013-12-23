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
    // !!! Android has no dedicated listbox - will have to use ListView (do we still want to maintain a simplem listbox implementation
    //     with ListView underneath for platform parity?
    //
    class AndroidListBoxWrapper : AndroidControlWrapper
    {
        public AndroidListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listbox element");

            ListView listView = new ListView(((AndroidControlWrapper)parent).Control.Context);
            this._control = listView;

            // !!! Need to implementat an Adapter/BaseAdapter than can feed the items to the list and make the item views

            applyFrameworkElementDefaults(listView);
        }
    }
}