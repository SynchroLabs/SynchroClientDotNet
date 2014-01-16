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
    // Alignment, margins, and padding overview...
    //
    // http://msdn.microsoft.com/en-us/library/ms751709(v=vs.110).aspx
    //

    class WinStackPanelWrapper : WinControlWrapper
    {
        StackPanel _stackPanel;
        protected HorizontalAlignment _hAlign;
        protected VerticalAlignment _vAlign;

        public HorizontalAlignment HorizontalAlignment { get { return _hAlign; } set { _hAlign = value; updateContentAlignment(); } }
        public VerticalAlignment VerticalAlignment { get { return _vAlign; } set { _vAlign = value; updateContentAlignment(); } }

        public WinStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stackpanel element");
            _stackPanel = new StackPanel();
            this._control = _stackPanel;

            applyFrameworkElementDefaults(_stackPanel);

            processElementProperty((string)controlSpec["orientation"], value => _stackPanel.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

            // Win/WinPhone support individual content item alignment in stack panels, but Android does not, so for now we're 
            // just going to be dumb like Android and align all items the same way.  If we did add item alignment support back at
            // some point, this attribute could still serve as the default item alignment.
            //
            processElementProperty((string)controlSpec["alignContentH"], value => this.HorizontalAlignment = ToHorizontalAlignment(value, HorizontalAlignment.Left), HorizontalAlignment.Left);
            processElementProperty((string)controlSpec["alignContentV"], value => this.VerticalAlignment = ToVerticalAlignment(value, VerticalAlignment.Center), VerticalAlignment.Center);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // childControlWrapper.processElementProperty((string)childControlSpec["align"], value => childControlWrapper.Control.HorizontalAlignment = ToHorizontalAlignment(value));
                    childControlWrapper.Control.HorizontalAlignment = _hAlign;
                    // childControlWrapper.processElementProperty((string)childControlSpec["align"], value => childControlWrapper.Control.VerticalAlignment = ToVerticalAlignment(value));
                    childControlWrapper.Control.VerticalAlignment = _vAlign;

                    _stackPanel.Children.Add(childControlWrapper.Control);
                });
            }
        }

        void updateContentAlignment()
        {
            foreach (Control child in _stackPanel.Children)
            {
                child.HorizontalAlignment = _hAlign;
                child.VerticalAlignment = _vAlign;
            }
        }
    }
}
