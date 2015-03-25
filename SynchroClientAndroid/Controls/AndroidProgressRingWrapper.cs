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
using SynchroCore;

namespace SynchroClientAndroid.Controls
{
    class AndroidProgressRingWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidProgressRingWrapper");

        public AndroidProgressRingWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating progress ring element");

            ProgressBar bar = new ProgressBar(((AndroidControlWrapper)parent).Control.Context);
            bar.Indeterminate = true;

            this._control = bar;

            applyFrameworkElementDefaults(bar);

            // processElementProperty((string)controlSpec["value"], value => button.Text = ToString(value));
        }
    }
}