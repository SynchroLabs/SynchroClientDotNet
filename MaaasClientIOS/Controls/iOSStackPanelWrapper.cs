using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    class iOSStackPanelWrapper : iOSControlWrapper
    {
        UIView _view;
        bool _isHorizontal = true;
        int _padding = 10;
        float _currTop;
        float _currLeft;

        public iOSStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stack panel element");

            _view = new UIView();
            this._control = _view;

            // !!! Sizing/position of stack panel needs some work...
            processElementDimensions(controlSpec, 150, 50);
            _view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleBottomMargin;

            applyFrameworkElementDefaults(_view);

            if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
            {
                _isHorizontal = false;
            }

            _currTop = _padding;
            _currLeft = _padding;

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    // !!! Worlds worst stackpanel
                    RectangleF frame = childControlWrapper.Control.Frame;
                    frame.X = _currLeft;
                    frame.Y = _currTop;
                    if (_isHorizontal)
                    {
                        _currLeft += childControlWrapper.Control.Bounds.Width + _padding;
                    }
                    else
                    {
                        _currTop += childControlWrapper.Control.Bounds.Height + _padding;
                    }
                    childControlWrapper.Control.Frame = frame;

                    _view.AddSubview(childControlWrapper.Control);
                });
            }
        }
    }
}