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
    class iOSTextBlockWrapper : iOSControlWrapper
    {
        public iOSTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text block element with text of: " + controlSpec["value"]);

            UILabel textBlock = new UILabel();
            this._control = textBlock;

            processElementDimensions(controlSpec, 150, 50);
            //textBlock.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleBottomMargin;

            applyFrameworkElementDefaults(textBlock);

            //processElementProperty((string)controlSpec["value"], value => textBlock.Text = ToString(value));
            processElementProperty((string)controlSpec["value"], value => 
            {
                // !!! We really only want to size to fix the height and/or width if not specied expicitly
                textBlock.Text = ToString(value);
                textBlock.SizeToFit();
            });
        }
    }
}