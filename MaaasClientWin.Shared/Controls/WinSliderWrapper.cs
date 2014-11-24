using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace MaaasClientWin.Controls
{
    class WinSliderWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinSliderWrapper");

        public WinSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            RangeBase rangeControl = null;

            if ((string)controlSpec["control"] == "progressbar")
            {
                rangeControl = new ProgressBar();
            }
            else
            {
                rangeControl = new Slider();
                ((Slider)rangeControl).Orientation = Orientation.Horizontal; // iOS/Android only support horizontal, so we limit this for now...
            }

            this._control = rangeControl;
            
            applyFrameworkElementDefaults(rangeControl);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return rangeControl.Value; }, value => rangeControl.Value = ToDouble(value)))
            {
                processElementProperty(controlSpec["value"], value => rangeControl.Value = ToDouble(value));
            }

            processElementProperty(controlSpec["minimum"], value => rangeControl.Minimum = ToDouble(value));
            processElementProperty(controlSpec["maximum"], value => rangeControl.Maximum = ToDouble(value));

            rangeControl.ValueChanged += slider_ValueChanged;  
        }

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}
