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
    class TextBlockFontSetter : iOSFontSetter
    {
        UILabel _label;

        public TextBlockFontSetter(UILabel label) : base(label.Font)
        {
            _label = label;
        }

        public override void setFont(UIFont font)
        {
            _label.Font = font;
            _label.SizeToFit(); // Might want to do this conditionally based on how control size was specified.
        }
    }

    class iOSTextBlockWrapper : iOSControlWrapper
    {
        public iOSTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text block element with text of: " + controlSpec["value"]);

            UILabel textBlock = new UILabel();
            this._control = textBlock;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(textBlock);

            processElementProperty((string)controlSpec["foreground"], value =>
            {
                ColorARGB colorArgb = ControlWrapper.getColor(ToString(value));
                UIColor color = UIColor.FromRGBA(colorArgb.r, colorArgb.g, colorArgb.b, colorArgb.a);
                textBlock.TextColor = color;
            });

            processFontAttribute(controlSpec, new TextBlockFontSetter(textBlock));

            // !!! ?
            /*
            processElementProperty((string)controlSpec["fontsize"], value => 
            {
                UIFont font = UIFont.FromName(textBlock.Font.Name, (float)ToDeviceUnitsFromTypographicPoints(value));
                textBlock.Font = font;
                textBlock.SizeToFit();
            });
             */

            processElementProperty((string)controlSpec["value"], value => 
            {
                // !!! We really only want to size to fix the height and/or width if not specied expicitly
                textBlock.Text = ToString(value);
                textBlock.SizeToFit();
            });
        }
    }
}