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
using SynchroCore;
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

            // http://developer.android.com/guide/webapps/webview.html
            webView.SetWebViewClient(new WebViewClient());

            applyFrameworkElementDefaults(webView);

            // !!! TODO - Android Web View
            processElementProperty(controlSpec["contents"], value => webView.LoadData(ToString(value), "text/html; charset=UTF-8", null));
            processElementProperty(controlSpec["url"], value => webView.LoadUrl(ToString(value)));
        }
    }
}