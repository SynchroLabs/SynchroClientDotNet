using MaaasCore;
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
        static Logger logger = Logger.GetLogger("WinTextBlockWrapper");

        public WinTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating text element with value of: {0}", controlSpec["value"]);
            TextBlock textBlock = new TextBlock();
            textBlock.TextWrapping = TextWrapping.Wrap;

            this._control = textBlock;

            applyFrameworkElementDefaults(textBlock);

            processElementProperty(controlSpec["value"], value => textBlock.Text = ToString(value));

            processElementProperty(controlSpec["ellipsize"], value =>
            {
                // Other trimming options:
                //
                //   TextTrimming.Clip;
                //   TextTrimming.WordEllipsis;
                //
                bool bEllipsize = ToBoolean(value);
                if (bEllipsize)
                {
                    textBlock.TextWrapping = TextWrapping.NoWrap;
                    textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
                }
                else
                {
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    textBlock.TextTrimming = TextTrimming.None;
                }
            });

            processElementProperty(controlSpec["textAlignment"], value =>
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

