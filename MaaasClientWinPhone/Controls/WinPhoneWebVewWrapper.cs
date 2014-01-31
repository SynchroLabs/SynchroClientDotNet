using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    // http://msdn.microsoft.com/en-US/library/windowsphone/develop/ff431797(v=vs.105).aspx
    //
    class WinPhoneWebViewWrapper : WinPhoneControlWrapper
    {
        public WinPhoneWebViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating web view element");
            WebBrowser webView = new WebBrowser();
            this._control = webView;

            applyFrameworkElementDefaults(webView);

            // !!! TODO
            processElementProperty((string)controlSpec["contents"], value => webView.NavigateToString(ToString(value)));
            processElementProperty((string)controlSpec["url"], value => webView.Navigate(new Uri(ToString(value))));

        }
    }
}