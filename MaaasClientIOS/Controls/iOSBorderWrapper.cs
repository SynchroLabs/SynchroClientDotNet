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
    public class PaddedView : UIView
    {
        protected UIEdgeInsets _padding = new UIEdgeInsets(0, 0, 0, 0);

        public PaddedView() : base()
        {
        }

        public UIEdgeInsets Padding
        {
            get { return _padding; }
            set
            {
                _padding = value;
                this.SetNeedsLayout();
            }
        }

        public float PaddingLeft
        {
            get { return _padding.Left; }
            set
            {
                _padding.Left = value;
                this.SetNeedsLayout();
            }
        }

        public float PaddingTop
        {
            get { return _padding.Top; }
            set
            {
                _padding.Top = value;
                this.SetNeedsLayout();
            }
        }

        public float PaddingRight
        {
            get { return _padding.Right; }
            set
            {
                _padding.Right = value;
                this.SetNeedsLayout();
            }
        }

        public float PaddingBottom
        {
            get { return _padding.Bottom; }
            set
            {
                _padding.Bottom = value;
                this.SetNeedsLayout();
            }
        }
    }

    public class PaddedViewThicknessSetter : ThicknessSetter
    {
        protected PaddedView _paddedView;

        public PaddedViewThicknessSetter(PaddedView paddedView)
        {
            _paddedView = paddedView;
        }

        public override void SetThicknessLeft(int thickness)
        {
            _paddedView.PaddingLeft = thickness;
        }

        public override void SetThicknessTop(int thickness)
        {
            _paddedView.PaddingTop = thickness;
        }

        public override void SetThicknessRight(int thickness)
        {
            _paddedView.PaddingRight = thickness;
        }

        public override void SetThicknessBottom(int thickness)
        {
            _paddedView.PaddingBottom = thickness;
        }
    }

    class BorderView : PaddedView
    {
        iOSControlWrapper _controlWrapper;
        protected UIView _childView = null;

        public BorderView(iOSControlWrapper controlWrapper) : base()
        {
            _controlWrapper = controlWrapper;
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
                UIEdgeInsets insets = new UIEdgeInsets(
                    this.Layer.BorderWidth + _padding.Top,
                    this.Layer.BorderWidth + _padding.Left,
                    this.Layer.BorderWidth + _padding.Bottom,
                    this.Layer.BorderWidth + _padding.Right
                    );

                RectangleF childFrame = _childView.Frame;
                SizeF panelSize = this.Frame.Size;

                UIEdgeInsets margin = new UIEdgeInsets(0, 0, 0, 0);
                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(_childView);
                if (childControlWrapper != null)
                {
                    margin = childControlWrapper.Margin;
                }

                if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent)
                {
                    // Panel width will size to content
                    //
                    childFrame.X = insets.Left + margin.Left;
                    panelSize.Width = childFrame.X + childFrame.Width + insets.Right + margin.Right;
                }
                else 
                {
                    // Panel width is explicit, so align content using the content horizontal alignment (along with padding and margin)
                    //
                    childFrame.X = insets.Left + margin.Left;

                    if (childControlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent)
                    {
                        // Child will fill parent (less margins/padding)
                        //
                        childFrame.Width = panelSize.Width - (insets.Right + margin.Right);
                    }
                    else
                    {
                        // Align child in parent
                        //
                        if (childControlWrapper.HorizontalAlignment == HorizontalAlignment.Center)
                        {
                            // Ignoring margins on center for now.
                            childFrame.X = (panelSize.Width - childFrame.Width) / 2;
                        }
                        else if (childControlWrapper.HorizontalAlignment == HorizontalAlignment.Right)
                        {
                            childFrame.X = (panelSize.Width - childFrame.Width - insets.Right - margin.Right);
                        }
                    }
                }

                if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent)
                {
                    // Panel height will size to content
                    //
                    childFrame.Y = insets.Top + margin.Top;
                    panelSize.Height = childFrame.Y + childFrame.Height + insets.Bottom + margin.Bottom;
                }
                else if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.Explicit)
                {
                    // Panel height is explicit, so align content using the content vertical alignment (along with padding and margin)
                    //
                    childFrame.Y = insets.Top + margin.Top;

                    if (childControlWrapper.FrameProperties.HeightSpec == SizeSpec.FillParent)
                    {
                        // Child will fill parent (less margins/padding)
                        //
                        childFrame.Height = panelSize.Height - (insets.Bottom + margin.Bottom);
                    }
                    else
                    {
                        // Align child in parent
                        //
                        if (childControlWrapper.VerticalAlignment == VerticalAlignment.Center)
                        {
                            // Ignoring margins on center for now.
                            childFrame.Y = (panelSize.Height - childFrame.Height) / 2;
                        }
                        else if (childControlWrapper.VerticalAlignment == VerticalAlignment.Bottom)
                        {
                            childFrame.Y = (panelSize.Height - childFrame.Height - insets.Bottom - margin.Bottom);
                        }
                    }

                }

                // Update the content position
                //
                _childView.Frame = childFrame;

                // See if the border panel might have changed size (based on content)
                //
                if ((_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent) || (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent))
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

            BorderView border = new BorderView(this);  
            this._control = border;

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(border);

            // If border thickness or padding change, need to resize view to child...
            //
            processElementProperty((string)controlSpec["border"], value => border.Layer.BorderColor = ToColor(value).CGColor);
            processElementProperty((string)controlSpec["borderThickness"], value => border.BorderWidth = (float)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["cornerRadius"], value => border.Layer.CornerRadius = (float)ToDeviceUnits(value));
            processThicknessProperty(controlSpec["padding"], new PaddedViewThicknessSetter(border));

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