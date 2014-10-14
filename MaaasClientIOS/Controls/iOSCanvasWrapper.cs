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
    class iOSCanvasWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSCanvasWrapper");

        public iOSCanvasWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating canvas element");

            UIView canvas = new UIView();
            this._control = canvas;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(canvas);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    childControlWrapper.processElementProperty((string)childControlSpec["left"], value =>
                    {
                        RectangleF childFrame = childControlWrapper.Control.Frame;
                        childFrame.X = (float)ToDeviceUnits(value);
                        childControlWrapper.Control.Frame = childFrame;
                        // !!! Resize canvas to contain control
                    });
                    childControlWrapper.processElementProperty((string)childControlSpec["top"], value =>
                    {
                        RectangleF childFrame = childControlWrapper.Control.Frame;
                        childFrame.Y = (float)ToDeviceUnits(value);
                        childControlWrapper.Control.Frame = childFrame;
                        // !!! Resize canvas to contain control
                    });

                    canvas.AddSubview(childControlWrapper.Control);
                });
            }
        }
    }
}