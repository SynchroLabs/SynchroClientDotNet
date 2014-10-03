using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace SynchroClientAndroid.Controls
{
    class AndroidSliderWrapper : AndroidControlWrapper
    {
        // Since SeekBar only has a max range, we'll simulate the min/max
        //
        int _min = 0;
        int _max = 0;

        public AndroidSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating slider element");
            SeekBar seekBar = new SeekBar(((AndroidControlWrapper)parent).Control.Context);
            this._control = seekBar;

            applyFrameworkElementDefaults(seekBar);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return _min + seekBar.Progress; }, value => seekBar.Progress = (int)ToDouble(value) - _min))
            {
                processElementProperty((string)controlSpec["value"], value => seekBar.Progress = (int)ToDouble(value));
            }

            processElementProperty((string)controlSpec["minimum"], value => {
                _min = (int)ToDouble(value);
                updateSeekBarRange();
            });
            processElementProperty((string)controlSpec["maximum"], value => {
                _max = (int)ToDouble(value);
                updateSeekBarRange();
            });

            seekBar.ProgressChanged += seekBar_ProgressChanged;
        }

        void updateSeekBarRange()
        {
            SeekBar seekBar = (SeekBar)this.Control;

            int progress = seekBar.Progress;
            seekBar.Max = _max - _min;
            seekBar.Progress = progress;
        }

        void seekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}