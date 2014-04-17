using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinTextBlockWrapper : WinControlWrapper
    {
        public WinTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text element with value of: " + controlSpec["value"]);
            TextBlock textBlock = new TextBlock();
            textBlock.TextWrapping = TextWrapping.Wrap;

            this._control = textBlock;

            applyFrameworkElementDefaults(textBlock);

            processElementProperty((string)controlSpec["value"], value => textBlock.Text = ToString(value));

            processElementProperty((string)controlSpec["textAlignment"], value =>
            {
                String alignString = ToString(value);
                if (alignString == "Left")
                {
                    textBlock.TextAlignment = TextAlignment.Left;
                }
                if (alignString == "Center")
                {
                    textBlock.TextAlignment = TextAlignment.Center;
                }
                else if (alignString == "Right")
                {
                    textBlock.TextAlignment = TextAlignment.Right;
                }
            });

        }
    }
}

