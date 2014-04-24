using MaaasCore;
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
    class WinPhoneTextBlockWrapper : WinPhoneControlWrapper
    {
        public WinPhoneTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text element with value of: " + controlSpec["value"]);
            TextBlock textBlock = new TextBlock();
            textBlock.TextWrapping = TextWrapping.Wrap;

            this._control = textBlock;

            applyFrameworkElementDefaults(textBlock);

            processElementProperty((string)controlSpec["value"], value => textBlock.Text = ToString(value));

            processElementProperty((string)controlSpec["ellipsize"], value =>
            {
                bool bEllipsize = ToBoolean(value);
                if (bEllipsize)
                {
                    textBlock.TextWrapping = TextWrapping.NoWrap;
                    textBlock.TextTrimming = TextTrimming.WordEllipsis;
                }
                else
                {
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.TextTrimming = TextTrimming.None;
                }
            });

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