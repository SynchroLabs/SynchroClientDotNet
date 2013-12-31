using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace MaaasClientIOS.Controls
{
    class iOSRectangleWrapper : iOSControlWrapper
    {
        public iOSRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating rectangle element");

            UIView rect = new UIImageView();            
            this._control = rect;

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(rect);
            processElementProperty((string)controlSpec["fill"], value => rect.BackgroundColor = ToColor(value));
        }
    }
}