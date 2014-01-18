using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasClientWinPhone.Controls
{
    class WinPhonePickerWrapper : WinPhoneControlWrapper
    {
        public WinPhonePickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");
            ListPicker picker = new ListPicker();
            this._control = picker;

            applyFrameworkElementDefaults(picker);

            // !!!
        }
    }
}
