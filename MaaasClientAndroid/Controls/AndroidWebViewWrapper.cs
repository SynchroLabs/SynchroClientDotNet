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
using Android.Webkit;

namespace SynchroClientAndroid.Controls
{
    // http://developer.android.com/reference/android/webkit/WebView.html
    //
    class AndroidWebViewWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidWebViewWrapper");

        public AndroidWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating web view button");
            WebView webView = new WebView(((AndroidControlWrapper)parent).Control.Context);
            this._control = webView;

            applyFrameworkElementDefaults(webView);

            // !!! TODO - Android Web View
            processElementProperty((string)controlSpec["contents"], value => webView.LoadData(ToString(value), "text/html; charset=UTF-8", null));
            processElementProperty((string)controlSpec["url"], value => webView.LoadUrl(ToString(value)));
        }
    }
}