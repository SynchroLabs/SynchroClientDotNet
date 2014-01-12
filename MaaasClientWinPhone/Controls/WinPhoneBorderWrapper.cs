using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneBorderWrapper : WinPhoneControlWrapper
    {
        public WinPhoneBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
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
            // "background" color handled by base class

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
