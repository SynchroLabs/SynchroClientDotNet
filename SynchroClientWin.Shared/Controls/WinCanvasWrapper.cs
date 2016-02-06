using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinCanvasWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinCanvasWrapper");

        public WinCanvasWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            Canvas canvas = new Canvas();
            this._control = canvas;

            applyFrameworkElementDefaults(canvas);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // We need to capture and potentially bind some attributes on the added child controls here in the context of the parent...
                    //
                    childControlWrapper.processElementProperty(childControlSpec, "top", value => Canvas.SetTop(childControlWrapper.Control, ToDeviceUnits(value)));
                    childControlWrapper.processElementProperty(childControlSpec, "left", value => Canvas.SetLeft(childControlWrapper.Control, ToDeviceUnits(value)));
                    childControlWrapper.processElementProperty(childControlSpec, "zindex", value => Canvas.SetZIndex(childControlWrapper.Control, (int)ToDouble(value)));
                    canvas.Children.Add(childControlWrapper.Control);
                });
            }
        }
    }
}
