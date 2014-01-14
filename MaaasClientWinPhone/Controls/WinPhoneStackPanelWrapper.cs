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
    class WinPhoneStackPanelWrapper : WinPhoneControlWrapper
    {
        public HorizontalAlignment ToHorizontalAlignment(object value, HorizontalAlignment defaultAlignment = HorizontalAlignment.Left)
        {
            HorizontalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Left")
            {
                alignment = HorizontalAlignment.Left;
            }
            if (alignmentValue == "Right")
            {
                alignment = HorizontalAlignment.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = HorizontalAlignment.Center;
            }
            return alignment;
        }

        public VerticalAlignment ToVerticalAlignment(object value, VerticalAlignment defaultAlignment = VerticalAlignment.Top)
        {
            VerticalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Top")
            {
                alignment = VerticalAlignment.Top;
            }
            if (alignmentValue == "Right")
            {
                alignment = VerticalAlignment.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = VerticalAlignment.Center;
            }
            return alignment;
        }

        HorizontalAlignment _hAlign;
        VerticalAlignment _vAlign;

        public WinPhoneStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stackpanel element");
            StackPanel stackPanel = new StackPanel();
            this._control = stackPanel;

            applyFrameworkElementDefaults(stackPanel);

            // Static
            Orientation orientation = Orientation.Horizontal;
            if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
            {
                orientation = Orientation.Vertical;
            }
            stackPanel.Orientation = orientation;

            // Win/WinPhone support individual content item alignment in stack panels, but Android does not, so for now we're 
            // just going to be dumb like Android and align all items the same way.  If we did add item alignment support back at
            // some point, this attribute could still serve as the default item alignment.
            //
            if (orientation == Orientation.Vertical)
            {
                _hAlign = ToHorizontalAlignment(controlSpec["alignContent"]);
            }
            else
            {
                _vAlign = ToVerticalAlignment(controlSpec["alignContent"]);
            }

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    if (orientation == Orientation.Vertical)
                    {
                        // childControlWrapper.processElementProperty((string)childControlSpec["align"], value => childControlWrapper.Control.HorizontalAlignment = ToHorizontalAlignment(value));
                        childControlWrapper.Control.HorizontalAlignment = _hAlign;
                    }
                    else
                    {
                        // childControlWrapper.processElementProperty((string)childControlSpec["align"], value => childControlWrapper.Control.VerticalAlignment = ToVerticalAlignment(value));
                        childControlWrapper.Control.VerticalAlignment = _vAlign;
                    }

                    stackPanel.Children.Add(childControlWrapper.Control);
                });
            }
        }
    }
}