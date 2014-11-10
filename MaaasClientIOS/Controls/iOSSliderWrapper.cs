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
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return slider.Value; }, value => setValue((float)ToDouble(value))))
            {
                processElementProperty((string)controlSpec["value"], value => setValue((float)ToDouble(value)));
            }

            processElementProperty((string)controlSpec["minimum"], value => setMin((float)ToDouble(value)));
            processElementProperty((string)controlSpec["maximum"], value => setMax((float)ToDouble(value)));

            slider.ValueChanged += slider_ValueChanged;
        }

        void slider_ValueChanged(object sender, EventArgs e)
        {
            updateValueBindingForAttribute("value");
        }

        // If you set the slider "Value" to a value outside of the current min/max range, it clips the value to the current min/max range.
        // This is a problem, as we might set the value, and then subsequently set the range (which defaults to 0/1), in which case we lose
        // the original attempt to set the value.  To avoid this, we track what we attempted to set the value to, and we fix it each time
        // we update the range (as needed).
        //
        float _value;

        void setMin(float min)
        {
            UISlider slider = (UISlider)this._control;
            bool needsValueUpdate = _value < slider.MinValue;
            slider.MinValue = min;
            if (needsValueUpdate)
            {
                slider.Value = _value;
            }
        }

        void setMax(float max)
        {
            UISlider slider = (UISlider)this._control;
            bool needsValueUpdate = _value > slider.MaxValue;
            slider.MaxValue = max;
            if (needsValueUpdate)
            {
                slider.Value = _value;
            }
        }

        void setValue(float value)
        {
            UISlider slider = (UISlider)this._control;
            _value = value;
            slider.Value = value;
        }
    }
}