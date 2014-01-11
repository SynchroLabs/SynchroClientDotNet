using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneScrollWrapper : WinPhoneControlWrapper
    {
        public WinPhoneScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating scroll element");
            ScrollViewer scroller = new ScrollViewer();
            this._control = scroller;

            if ((controlSpec["orientation"] == null) || ((string)controlSpec["orientation"] != "horizontal"))
            {
                // Vertical (default)
                scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                // Horizontal
                scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            applyFrameworkElementDefaults(scroller);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    scroller.Content = childControlWrapper.Control;
                });
            }
        }
    }
}

