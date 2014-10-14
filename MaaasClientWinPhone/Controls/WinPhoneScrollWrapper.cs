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
        static Logger logger = Logger.GetLogger("WinPhoneScrollWrapper");

        protected ScrollViewer _scroller;

        public WinPhoneScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating scroll element");
            _scroller = new ScrollViewer();
            this._control = _scroller;

            processElementProperty((string)controlSpec["orientation"], value => setOrientation(ToOrientation(value, Orientation.Vertical)), Orientation.Vertical);

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
                _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                _scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                _scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        }
    }
}

