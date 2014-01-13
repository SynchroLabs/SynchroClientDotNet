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
    class BorderView : UIView
    {
        protected UIView _childView = null;
        protected float _padding = 0;

        public BorderView() : base()
        {
        }

        public float BorderWidth
        {
            get { return this.Layer.BorderWidth; }
            set
            {
                this.Layer.BorderWidth = value;
                this.SetNeedsLayout();
            }
        }

        public float Padding
        {
            get { return _padding; }
            set
            {
                _padding = value;
                this.SetNeedsLayout();
            }
        }

        public override void AddSubview(UIView view)
        {
            _childView = view;
            base.AddSubview(view);
        }

        public override void LayoutSubviews()
        {
            // Util.debug("BorderView - Layout subviews");

            if (_childView != null)
            {
                float borderPlusPadding = this.Layer.BorderWidth + _padding;

                // Position the child considering border width and padding...
                //
                RectangleF childFrame = _childView.Frame;
                childFrame.X = borderPlusPadding;
                childFrame.Y = borderPlusPadding;
                _childView.Frame = childFrame;

                // Resize the panel (border) to contain the control...
                //
                SizeF panelSize = this.Frame.Size;
                panelSize.Width = childFrame.X + childFrame.Width + borderPlusPadding;
                panelSize.Height = childFrame.Y + childFrame.Height + borderPlusPadding;
                RectangleF panelFrame = this.Frame;
                panelFrame.Size = panelSize;
                this.Frame = panelFrame;

                if (this.Superview != null)
                {
                    this.Superview.SetNeedsLayout();
                }
            }

            base.LayoutSubviews();
        }
    }

    class iOSBorderWrapper : iOSControlWrapper
    {
        public iOSBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");

            BorderView border = new BorderView();  
            this._control = border;

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(border);

            // If border thickness or padding change, need to resize view to child...
            //
            processElementProperty((string)controlSpec["border"], value => border.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderthickness"], value => border.BorderWidth = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["cornerradius"], value => border.Layer.CornerRadius = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["padding"], value => border.Padding = (float)ToDeviceUnits(value)); // !!! Simple value only for now

            // "background" color handled by base class

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    border.AddSubview(childControlWrapper.Control);
                });
            }
        }
    }
}