﻿using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneSliderWrapper : WinPhoneControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPhoneSliderWrapper");

        public WinPhoneSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating slider element");
            Slider slider = new Slider();
            this._control = slider;

            slider.Orientation = Orientation.Horizontal; // iOS/Android only support horizontal, so we limit this for now...

            applyFrameworkElementDefaults(slider);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return slider.Value; }, value => slider.Value = ToDouble(value)))
            {
                processElementProperty((string)controlSpec["value"], value => slider.Value = ToDouble(value));
            }

            processElementProperty((string)controlSpec["minimum"], value => slider.Minimum = ToDouble(value));
            processElementProperty((string)controlSpec["maximum"], value => slider.Maximum = ToDouble(value));

            slider.ValueChanged += slider_ValueChanged;
        }

        private void slider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}
