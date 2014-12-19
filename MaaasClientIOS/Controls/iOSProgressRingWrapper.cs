using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;

namespace MaaasClientIOS.Controls
{
    class iOSProgressRingWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSProgressRingWrapper");

        public iOSProgressRingWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating progress ring element");

            UIActivityIndicatorView progress = new UIActivityIndicatorView();
            progress.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.Gray;

            this._control = progress;

            processElementDimensions(controlSpec, 50, 50);

            applyFrameworkElementDefaults(progress);

            processElementProperty(controlSpec["value"], value => 
            {
                bool animate = ToBoolean(value);
                if (animate && !progress.IsAnimating)
                {
                    progress.StartAnimating();
                }
                else if (!animate && progress.IsAnimating)
                {
                    progress.StopAnimating();
                }
            });
        }
    }
}