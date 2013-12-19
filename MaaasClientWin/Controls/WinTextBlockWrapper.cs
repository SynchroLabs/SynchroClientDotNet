using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaasClient.Controls
{
    class WinTextBlockWrapper : WinControlWrapper
    {
        public WinTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text element with value of: " + controlSpec["value"]);
            TextBlock textBlock = new TextBlock();
            this._control = textBlock;

            applyFrameworkElementDefaults(textBlock);

            processElementProperty((string)controlSpec["value"], value => textBlock.Text = ToString(value));
        }
    }
}

