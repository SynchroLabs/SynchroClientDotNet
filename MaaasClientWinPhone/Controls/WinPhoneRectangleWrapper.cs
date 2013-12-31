using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneRectangleWrapper : WinPhoneControlWrapper
    {
        public WinPhoneRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
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
