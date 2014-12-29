using SynchroCore;
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
        static Logger logger = Logger.GetLogger("WinBorderWrapper");

        protected Border _border;

        public WinBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            _border = new Border();
            this._control = _border;

            applyFrameworkElementDefaults(_border);

            processElementProperty(controlSpec["border"], value => _border.BorderBrush = ToBrush(value));
            processThicknessProperty(controlSpec["borderThickness"], () => _border.BorderThickness, value => _border.BorderThickness = (Thickness)value);
            processElementProperty(controlSpec["cornerRadius"], value => _border.CornerRadius = new CornerRadius(ToDouble(value)));
            processThicknessProperty(controlSpec["padding"], () => _border.Padding, value => _border.Padding = (Thickness)value);
            // "background" color handled by base class

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    _border.Child = childControlWrapper.Control;
                });
            }
        }
    }
}
