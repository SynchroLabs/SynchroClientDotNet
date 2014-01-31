using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    // https://developer.apple.com/library/ios/documentation/uikit/reference/UIWebView_Class/Reference/Reference.html
    //
    class iOSWebViewWrapper : iOSControlWrapper
    {
        public iOSWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating web view element");

            UIWebView webView = new UIWebView();
            this._control = webView;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(webView);

            // !!! TODO
            processElementProperty((string)controlSpec["contents"], value => webView.LoadHtmlString(ToString(value), null));
            processElementProperty((string)controlSpec["url"], value => webView.LoadRequest(new NSUrlRequest(new NSUrl(ToString(value)))));
        }
    }
}