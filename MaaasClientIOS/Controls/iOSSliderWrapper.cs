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
    class iOSSliderWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSSliderWrapper");

        public iOSSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating slider element");

            UISlider slider = new UISlider();
            this._control = slider;

            processElementDimensions(controlSpec, 150, 50);

            applyFrameworkElementDefaults(slider);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return slider.Value; }, value => slider.Value = (float)ToDouble(value)))
            {
                processElementProperty((string)controlSpec["value"], value => slider.Value = (float)ToDouble(value));
            }

            processElementProperty((string)controlSpec["minimum"], value => slider.MinValue = (float)ToDouble(value));
            processElementProperty((string)controlSpec["maximum"], value => slider.MaxValue = (float)ToDouble(value));

            slider.ValueChanged += slider_ValueChanged;
        }

        void slider_ValueChanged(object sender, EventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}