using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneCanvasWrapper : WinPhoneControlWrapper
    {
        public WinPhoneCanvasWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating canvas element");
            Canvas canvas = new Canvas();
            this._control = canvas;

            applyFrameworkElementDefaults(canvas);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // We need to capture and potentially bind some attributes on the added child controls here in the context of the parent...
                    //
                    childControlWrapper.processElementProperty((string)childControlSpec["top"], value => Canvas.SetTop(childControlWrapper.Control, ToDouble(value)));
                    childControlWrapper.processElementProperty((string)childControlSpec["left"], value => Canvas.SetLeft(childControlWrapper.Control, ToDouble(value)));
                    childControlWrapper.processElementProperty((string)childControlSpec["zindex"], value => Canvas.SetZIndex(childControlWrapper.Control, (int)ToDouble(value)));
                    canvas.Children.Add(childControlWrapper.Control);
                });
            }

        }
    }
}
