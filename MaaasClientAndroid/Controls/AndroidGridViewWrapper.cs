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
using Android.Webkit;
using SynchroCore;

namespace SynchroClientAndroid.Controls
{
    // http://developer.android.com/guide/topics/ui/layout/gridview.html
    //
    // http://docs.xamarin.com/guides/android/user_interface/grid_view/
    //
    class AndroidGridViewWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidGridViewWrapper");

        public AndroidGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating grid view button");
            GridView gridView = new GridView(((AndroidControlWrapper)parent).Control.Context);
            this._control = gridView;

            applyFrameworkElementDefaults(gridView);

            // !!! TODO - Implement Android Grid View
        }
    }
}