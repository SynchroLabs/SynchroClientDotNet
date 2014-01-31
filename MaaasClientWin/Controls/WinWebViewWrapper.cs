using MaaasCore;
using Newtonsoft.Json.Linq;
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
        public WinWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating web view element");
            WebView webView = new WebView();
            this._control = webView;

            applyFrameworkElementDefaults(webView);

            // !!! TODO
            processElementProperty((string)controlSpec["contents"], value => webView.NavigateToString(ToString(value)));
            processElementProperty((string)controlSpec["url"], value => webView.Navigate(new Uri(ToString(value))));
        }
    }
}