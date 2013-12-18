using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaasClient.Controls
{
    class WinSliderWrapper : WinControlWrapper
    {
        public WinSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating slider element");
            Slider slider = new Slider();
            this._control = slider;
            
            applyFrameworkElementDefaults(slider);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return slider.Value; }, value => slider.Value = Converter.ToDouble(value)))
            {
                processElementProperty((string)controlSpec["value"], value => slider.Value = Converter.ToDouble(value));
            }

            processElementProperty((string)controlSpec["minimum"], value => slider.Minimum = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["maximum"], value => slider.Maximum = Converter.ToDouble(value));

            slider.ValueChanged += slider_ValueChanged;  
        }

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}
