using MaaasCore;
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
        static Logger logger = Logger.GetLogger("WinScrollWrapper");

        protected ScrollViewer _scroller;

        public WinScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            // ScrollViewer - http://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.scrollviewer.aspx
            //
            logger.Debug("Creating scroll element");
            _scroller = new ScrollViewer();
            this._control = _scroller;

            processElementProperty(controlSpec["orientation"], value => setOrientation(ToOrientation(value, Orientation.Vertical)), Orientation.Vertical);

            applyFrameworkElementDefaults(_scroller);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    _scroller.Content = childControlWrapper.Control;
                });
            }
        }

        public void setOrientation(Orientation orientation)
        {
            if (orientation == Orientation.Vertical)
            {
                _scroller.VerticalScrollMode = ScrollMode.Enabled;
                _scroller.HorizontalScrollMode = ScrollMode.Disabled;
                _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                _scroller.VerticalScrollMode = ScrollMode.Disabled;
                _scroller.HorizontalScrollMode = ScrollMode.Enabled;
                _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
    }
}
