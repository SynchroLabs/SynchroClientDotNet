using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinBorderWrapper : WinControlWrapper
    {
        public WinBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");
            Border border = new Border();
            this._control = border;

            applyFrameworkElementDefaults(border);

            processElementProperty((string)controlSpec["border"], value => border.BorderBrush = ToBrush(value));
            processThicknessProperty(controlSpec["borderthickness"], value => border.BorderThickness = (Thickness)value);
            processElementProperty((string)controlSpec["cornerradius"], value => border.CornerRadius = new CornerRadius(ToDouble(value)));
            processThicknessProperty(controlSpec["padding"], value => border.Padding = (Thickness)value);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    border.Child = childControlWrapper.Control;
                });
            }
        }
    }
}
