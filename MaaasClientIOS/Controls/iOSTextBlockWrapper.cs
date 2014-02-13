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

            processElementProperty((string)controlSpec["value"], value => 
            {
                textBlock.Text = ToString(value);
                textBlock.SizeToFit();

                // !!! We really only want to size to fit the height and/or width if not specied expicitly.  If we had a way to 
                //     track the explicit vs default height/width setting, we could use something like the below to compute the
                //     other dimension...
                //
                /*
                RectangleF frame = textBlock.Frame;
                SizeF size = textBlock.SizeThatFits(frame.Size);
                frame.Size = size;
                textBlock.Frame = frame;
                 */
            });
        }
    }
}