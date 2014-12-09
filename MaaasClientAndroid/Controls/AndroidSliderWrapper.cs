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

namespace SynchroClientAndroid.Controls
{
    class AndroidSliderWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidSliderWrapper");

        // Since SeekBar only has a max range, we'll simulate the min/max
        //
        int _min = 0;
        int _max = 0;
        int _progress = 0;

        void upadateBar()
        {
            ProgressBar bar = (ProgressBar)this.Control;
            bar.Max = _max - _min;
            bar.Progress = _progress - _min;
        }

        void setMin(double min)
        {
            _min = (int)min;
            upadateBar();
        }

        void setMax(double max)
        {
            _max = (int)max;
            upadateBar();
        }

        void setProgress(double progress)
        {
            _progress = (int)progress;
            upadateBar();
        }

        double getProgress()
        {
            ProgressBar bar = (ProgressBar)this.Control;
            return bar.Progress + _min;
        }

        public AndroidSliderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            ProgressBar bar = null;

            if ((string)controlSpec["control"] == "progressbar")
            {
                logger.Debug("Creating progress bar element");
                bar = new ProgressBar(((AndroidControlWrapper)parent).Control.Context, null, Android.Resource.Attribute.ProgressBarStyleHorizontal);
                bar.Indeterminate = false;
            }
            else
            {
                logger.Debug("Creating slider element");
                bar = new SeekBar(((AndroidControlWrapper)parent).Control.Context);
                ((SeekBar)bar).ProgressChanged += seekBar_ProgressChanged;
            }

            this._control = bar;

            applyFrameworkElementDefaults(bar);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new MaaasCore.JValue(getProgress()); }, value => setProgress(ToDouble(value))))
            {
                processElementProperty(controlSpec["value"], value => setProgress(ToDouble(value)));
            }

            processElementProperty(controlSpec["minimum"], value => setMin(ToDouble(value)));
            processElementProperty(controlSpec["maximum"], value => setMax(ToDouble(value)));
        }

        void seekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            updateValueBindingForAttribute("value");
        }
    }
}