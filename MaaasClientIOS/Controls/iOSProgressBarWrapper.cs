using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;

namespace MaaasClientIOS.Controls
{
    class iOSProgressBarWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSProgressBarWrapper");

        double _min = 0.0;
        double _max = 1.0;

        protected double GetProgress(double progress)
        {
            if ((_max <= _min) || (progress <= _min))
            {
                return 0.0;
            }
            else if (progress >= _max)
            {
                return 1.0;
            }

            return (progress - _min) / (_max - _min);
        }

        public iOSProgressBarWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating progress bar element");

            UIProgressView progress = new UIProgressView();
            this._control = progress;

            processElementDimensions(controlSpec, 150, 25);

            applyFrameworkElementDefaults(progress);

            processElementProperty(controlSpec["value"], value => progress.Progress = (float)GetProgress(ToDouble(value)));
            processElementProperty(controlSpec["minimum"], value => _min = ToDouble(value));
            processElementProperty(controlSpec["maximum"], value => _max = ToDouble(value));
        }
    }
}