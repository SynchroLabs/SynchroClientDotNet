using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace MaaasClientWin.Controls
{
    class WinRectangleWrapper : WinControlWrapper
    {
        public WinRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating rectangle element");
            Rectangle rect = new Rectangle();
            this._control = rect;

            applyFrameworkElementDefaults(rect);
            processElementProperty((string)controlSpec["fill"], value => rect.Fill = ToBrush(value));
        }
    }
}
