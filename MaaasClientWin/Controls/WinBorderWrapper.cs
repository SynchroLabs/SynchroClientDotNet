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
        protected Border _border;
        protected HorizontalAlignment _hAlign;
        protected VerticalAlignment _vAlign;

        public HorizontalAlignment HorizontalAlignment { get { return _hAlign; } set { _hAlign = value; updateContentAlignment(); } }
        public VerticalAlignment VerticalAlignment { get {return _vAlign; } set { _vAlign = value; updateContentAlignment(); } }

        public WinBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");
            _border = new Border();
            this._control = _border;

            applyFrameworkElementDefaults(_border);

            processElementProperty((string)controlSpec["border"], value => _border.BorderBrush = ToBrush(value));
            processThicknessProperty(controlSpec["borderThickness"], value => _border.BorderThickness = (Thickness)value);
            processElementProperty((string)controlSpec["cornerRadius"], value => _border.CornerRadius = new CornerRadius(ToDouble(value)));
            processThicknessProperty(controlSpec["padding"], value => _border.Padding = (Thickness)value);
            // "background" color handled by base class

            processElementProperty((string)controlSpec["alignContentH"], value => this.HorizontalAlignment = ToHorizontalAlignment(value, HorizontalAlignment.Center), HorizontalAlignment.Center);
            processElementProperty((string)controlSpec["alignContentV"], value => this.VerticalAlignment = ToVerticalAlignment(value, VerticalAlignment.Center), VerticalAlignment.Center);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    childControlWrapper.Control.HorizontalAlignment = _hAlign;
                    childControlWrapper.Control.VerticalAlignment = _vAlign;
                    _border.Child = childControlWrapper.Control;
                });
            }
        }

        void updateContentAlignment()
        {
            if (_border.Child != null)
            {
                ((Control)_border.Child).HorizontalAlignment = _hAlign;
                ((Control)_border.Child).VerticalAlignment = _vAlign;
            }
        }
    }
}
