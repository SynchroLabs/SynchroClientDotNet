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
    class iOSBorderWrapper : iOSControlWrapper
    {
        protected UIView _view = null;  
        protected UIView _childView = null;
        protected int _padding = 10;

        public iOSBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");

            _view = new UIView();  
            this._control = _view;

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(_view);

            processElementProperty((string)controlSpec["border"], value => _view.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderthickness"], value => _view.Layer.BorderWidth = (float)ToDeviceUnits(value));

            processElementProperty((string)controlSpec["fill"], value => _view.BackgroundColor = ToColor(value));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    _childView = childControlWrapper.Control;
                    _view.AddSubview(_childView);
                    sizeToChild();
                });
            }
        }

        protected void sizeToChild()
        {
            float borderPlusPadding = _view.Layer.BorderWidth + _padding;

            // Position the child considering border width and padding...
            //
            RectangleF childFrame = _childView.Frame;
            childFrame.X = borderPlusPadding;
            childFrame.Y = borderPlusPadding;
            _childView.Frame = childFrame;

            // Resize the panel (border) to contain the control...
            //
            SizeF panelSize = _view.Frame.Size;
            panelSize.Width = childFrame.X + childFrame.Width + borderPlusPadding;
            panelSize.Height = childFrame.Y + childFrame.Height + borderPlusPadding;
            RectangleF panelFrame = _view.Frame;
            panelFrame.Size = panelSize;
            _view.Frame = panelFrame;
        }
    }
}