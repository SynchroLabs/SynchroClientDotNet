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
    class RectangleView : UIView
    {
        protected UIColor _color = UIColor.Clear;

        public RectangleView() : base()
        {
        }

        public UIColor Color 
        {
            get { return _color; } 
            set 
            { 
                _color = value;
                this.SetNeedsDisplay();
            } 
        }

        public override void Draw(RectangleF rect)
        {
            base.Draw(rect);

            using (var context = UIGraphics.GetCurrentContext())
            {
                context.SetFillColor(_color.CGColor);
                context.FillRect(rect);
            }
        }
    }

    class iOSRectangleWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSRectangleWrapper");

        public iOSRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating rectangle element");

            RectangleView rect = new RectangleView();
            this._control = rect;

            rect.Layer.MasksToBounds = true; // So that fill color will stay inside of border (if any)

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(rect);

            processElementProperty((string)controlSpec["border"], value => rect.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderThickness"], value => rect.Layer.BorderWidth = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["cornerRadius"], value => rect.Layer.CornerRadius = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["fill"], value => rect.Color = ToColor(value));
        }
    }
}