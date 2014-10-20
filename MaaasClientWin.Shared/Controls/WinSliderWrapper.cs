﻿using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinSliderWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinSliderWrapper");

        public WinSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
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

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}