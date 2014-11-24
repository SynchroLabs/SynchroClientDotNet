using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    // https://developer.apple.com/library/ios/documentation/uikit/reference/UIWebView_Class/Reference/Reference.html
    //
    class iOSWebViewWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSWebViewWrapper");

        public iOSWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating web view element");

            UIWebView webView = new UIWebView();
            this._control = webView;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(webView);

            // !!! TODO - iOS Web View
            processElementProperty(controlSpec["contents"], value => webView.LoadHtmlString(ToString(value), null));
            processElementProperty(controlSpec["url"], value => webView.LoadRequest(new NSUrlRequest(new NSUrl(ToString(value)))));
        }
    }
}