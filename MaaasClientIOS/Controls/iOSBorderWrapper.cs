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
        protected float _padding = 0;

        public iOSBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");

            _view = new UIView();  
            this._control = _view;

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(_view);

            // If border thickness or padding change, need to resize view to child...
            //
            processElementProperty((string)controlSpec["border"], value => _view.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderthickness"], value => 
            {
                _view.Layer.BorderWidth = (float)ToDeviceUnits(value);
                this.sizeToChild();
            });
            processElementProperty((string)controlSpec["cornerradius"], value => _view.Layer.CornerRadius = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["padding"], value => // !!! Simple value only for now
            {
                _padding = (float)ToDeviceUnits(value);
                this.sizeToChild();
            }); 

            // "background" color handled by base class

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