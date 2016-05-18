using SynchroCore;
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
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            ProgressRing ring = new ProgressRing();
            this._control = ring;

#if WINDOWS_PHONE_APP
            // Default size is good in WinPhone
#else
            // Default size is tiny on Win, so we set it (this will be overridden by any specified size)
            ring.Height = this.ToDeviceUnits(50);
            ring.Width = this.ToDeviceUnits(50);
#endif

            applyFrameworkElementDefaults(ring);
 
            processElementProperty(controlSpec, "value", value => ring.IsActive = ToBoolean(value));
        }
    }
}
