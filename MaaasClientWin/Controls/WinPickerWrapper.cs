using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinPickerWrapper : WinControlWrapper
    {
        public WinPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");
            ComboBox picker = new ComboBox();
            this._control = picker;

            applyFrameworkElementDefaults(picker);
 
            // !!!
        }
    }
}
