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
        protected HorizontalAlignment _hAlign;
        protected VerticalAlignment _vAlign;
        protected FrameProperties _frameProps;

        public BorderView() : base()
        {
        }

        public FrameProperties FrameProperties { get { return _frameProps; } set { _frameProps = value; } }
        public HorizontalAlignment HorizontalAlignment { get { return _hAlign; } set { _hAlign = value; } }
        public VerticalAlignment VerticalAlignment { get { return _vAlign; } set { _vAlign = value; } }

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

                RectangleF childFrame = _childView.Frame;
                SizeF panelSize = this.Frame.Size;

                if (_frameProps.WidthSpecified)
                {
                    // Panel width is explicit, so align content using the content horizontal alignment (along with padding)
                    //
                    childFrame.X = borderPlusPadding;
                    if (_hAlign == HorizontalAlignment.Center)
                    {
                        childFrame.X = (panelSize.Width - childFrame.Width) / 2;
                    }
                    else if (_hAlign == HorizontalAlignment.Right)
                    {
                        childFrame.X = (panelSize.Width - childFrame.Width - borderPlusPadding);
                    }
                }
                else
                {
                    // Panel width will size to content
                    //
                    childFrame.X = borderPlusPadding;
                    panelSize.Width = childFrame.X + childFrame.Width + borderPlusPadding;
                }

                if (_frameProps.HeightSpecified)
                {
                    // Panel height is explicit, so align content using the content vertical alignment (along with padding)
                    //
                    childFrame.Y = borderPlusPadding;
                    if (_vAlign == VerticalAlignment.Center)
                    {
                        childFrame.Y = (panelSize.Height - childFrame.Height) / 2;
                    }
                    else if (_vAlign == VerticalAlignment.Bottom)
                    {
                        childFrame.Y = (panelSize.Height - childFrame.Height - borderPlusPadding);
                    }
                }
                else
                {
                    // Panel height will size to content
                    //
                    childFrame.Y = borderPlusPadding;
                    panelSize.Height = childFrame.Y + childFrame.Height + borderPlusPadding;
                }

                // Update the content position
                //
                _childView.Frame = childFrame;

                // See if the border panel might have changed size (based on content)
                //
                if (!_frameProps.WidthSpecified || !_frameProps.HeightSpecified)
                {
                    // See if the border panel actually did change size
                    //
                    if ((this.Frame.Width != panelSize.Width) || (this.Frame.Height != panelSize.Height))
                    {
                        // Resize the border panel to contain the control...
                        //
                        RectangleF panelFrame = this.Frame;
                        panelFrame.Size = panelSize;
                        this.Frame = panelFrame;

                        if (this.Superview != null)
                        {
                            this.Superview.SetNeedsLayout();
                        }
                    }
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

            border.FrameProperties = processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(border);

            // If border thickness or padding change, need to resize view to child...
            //
            processElementProperty((string)controlSpec["border"], value => border.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderThickness"], value => border.BorderWidth = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["cornerRadius"], value => border.Layer.CornerRadius = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["padding"], value => border.Padding = (float)ToDeviceUnits(value)); // !!! Simple value only for now

            // "background" color handled by base class

            processElementProperty((string)controlSpec["alignContentH"], value => border.HorizontalAlignment = ToHorizontalAlignment(value, HorizontalAlignment.Center), HorizontalAlignment.Center);
            processElementProperty((string)controlSpec["alignContentV"], value => border.VerticalAlignment = ToVerticalAlignment(value, VerticalAlignment.Center), VerticalAlignment.Center);

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