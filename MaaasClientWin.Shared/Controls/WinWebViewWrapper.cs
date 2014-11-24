using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    // http://msdn.microsoft.com/library/windows/apps/br227702
    //
    class WinWebViewWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinWebViewWrapper");

        public WinWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating web view element");
            WebView webView = new WebView();
            this._control = webView;

            applyFrameworkElementDefaults(webView);

            // !!! TODO - Windows Web View
            processElementProperty(controlSpec["contents"], value => webView.NavigateToString(ToString(value)));
            processElementProperty(controlSpec["url"], value => webView.Navigate(new Uri(ToString(value))));
        }
    }
}