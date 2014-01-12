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
    class WinScrollWrapper : WinControlWrapper
    {
        public WinScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            // ScrollViewer - http://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.scrollviewer.aspx
            //
            Util.debug("Creating scroll element");
            ScrollViewer scroller = new ScrollViewer();
            this._control = scroller;

            if ((controlSpec["orientation"] == null) || ((string)controlSpec["orientation"] != "horizontal"))
            {
                // Vertical (default)
                scroller.VerticalScrollMode = ScrollMode.Enabled;
                scroller.HorizontalScrollMode = ScrollMode.Disabled;
                scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                // Horizontal
                scroller.VerticalScrollMode = ScrollMode.Disabled;
                scroller.HorizontalScrollMode = ScrollMode.Enabled;
                scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
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
