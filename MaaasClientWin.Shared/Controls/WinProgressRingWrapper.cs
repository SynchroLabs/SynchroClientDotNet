using MaaasCore;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinProgressRingWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinProgressRingWrapper");

        public WinProgressRingWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            ProgressRing ring = new ProgressRing();
            this._control = ring;

            applyFrameworkElementDefaults(ring);
 
            processElementProperty(controlSpec["value"], value => ring.IsActive = ToBoolean(value));
        }
    }
}
