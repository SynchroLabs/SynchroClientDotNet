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
    class StackPanelView : PaddedView
    {
        protected iOSControlWrapper _controlWrapper;
        protected Orientation _orientation;
        protected float _spacing = 10;
        protected FrameProperties _frameProps;
        protected HorizontalAlignment _hAlign;
        protected VerticalAlignment _vAlign;

        public StackPanelView(iOSControlWrapper controlWrapper)
            : base()
        {
            _controlWrapper = controlWrapper;
        }

        public Orientation Orientation 
        { 
            get { return _orientation; } 
            set 
            {
                _orientation = value;
                this.SetNeedsLayout();
            } 
        }

        public FrameProperties FrameProperties { get { return _frameProps; } set { _frameProps = value; } }

        public HorizontalAlignment HorizontalAlignment 
        { 
            get { return _hAlign; } 
            set 
            { 
                _hAlign = value;
                this.SetNeedsLayout();
            } 
        }

        public VerticalAlignment VerticalAlignment 
        {
            get { return _vAlign; } 
            set 
            {
                _vAlign = value;
                this.SetNeedsLayout();
            } 
        }

        public override void AddSubview(UIView view)
        {
            base.AddSubview(view);
        }

        public override void LayoutSubviews()
        {
            // Util.debug("StackPanelView - Layout subviews");

            base.LayoutSubviews();

            // Determine the maximum subview size in each dimension (for item alignment later).
            //
            SizeF maxDimensions = new SizeF(0, 0);
            foreach (UIView childView in this.Subviews)
            {
                UIEdgeInsets margin = new UIEdgeInsets(0 ,0, 0, 0);
                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);
                if (childControlWrapper != null)
                {
                    margin = childControlWrapper.Margin;
                }

                maxDimensions.Width = Math.Max(maxDimensions.Width, childView.Frame.Width + margin.Left + margin.Right);
                maxDimensions.Height = Math.Max(maxDimensions.Height, childView.Frame.Height + margin.Top + margin.Bottom);
            }

            if (_frameProps.WidthSpecified)
            {
                maxDimensions.Width = this.Frame.Width;
            }
            if (_frameProps.HeightSpecified)
            {
                maxDimensions.Height = this.Frame.Height;
            }

            float _currTop = _padding.Top;
            float _currLeft = _padding.Left;

            SizeF newPanelSize = new SizeF(0, 0);

            UIEdgeInsets lastMargin = new UIEdgeInsets(0, 0, 0, 0);

            // Arrange the subviews (align as appropriate)
            //
            foreach (UIView childView in this.Subviews)
            {
                UIEdgeInsets margin = new UIEdgeInsets(0, 0, 0, 0);
                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);
                if (childControlWrapper != null)
                {
                    margin = childControlWrapper.Margin;
                }

                RectangleF childFrame = childView.Frame;

                if (_orientation == Orientation.Horizontal)
                {
                    // Set the horizontal position (considering margin overlap)
                    childFrame.X = _currLeft + Math.Max(lastMargin.Right, margin.Left);

                    // Set the vertical position based on aligment (default Top)
                    childFrame.Y = _currTop + margin.Top;
                    if (_vAlign == VerticalAlignment.Center)
                    {
                        // Should we consider margin when centering?  For now, we don't.
                        childFrame.Y = _currTop + ((maxDimensions.Height - childFrame.Height) / 2);
                    }
                    else if (_vAlign == VerticalAlignment.Bottom)
                    {
                        childFrame.Y = _currTop + (maxDimensions.Height - childFrame.Height) - margin.Bottom;
                    }
                    _currLeft = childFrame.X + childFrame.Width;
                }
                else // Orientation.Vertical
                {
                    // Set the vertical position (considering margin overlap)
                    childFrame.Y = _currTop + Math.Max(lastMargin.Bottom, margin.Top);

                    // Set the horizontal position based on aligment (default Left)
                    childFrame.X = _currLeft + margin.Left;
                    if (_hAlign == HorizontalAlignment.Center)
                    {
                        // Should we consider margin when centering?  For now, we don't.
                        childFrame.X = _currLeft + ((maxDimensions.Width - childFrame.Width) / 2);
                    }
                    else if (_hAlign == HorizontalAlignment.Right)
                    {
                        childFrame.X = _currLeft + (maxDimensions.Width - childFrame.Width) - margin.Right;
                    }
                    _currTop = childFrame.Y + childFrame.Height;
                }
                childView.Frame = childFrame;

                if ((childFrame.X + childFrame.Width + margin.Right) > newPanelSize.Width)
                {
                    newPanelSize.Width = childFrame.X + childFrame.Width + margin.Right;
                }
                if ((childFrame.Y + childFrame.Height + margin.Bottom) > newPanelSize.Height)
                {
                    newPanelSize.Height = childFrame.Y + childFrame.Height + margin.Bottom;
                }

                lastMargin = margin;
            }

            // Resize the stackpanel to contain the subview
            //
            newPanelSize.Height += _padding.Bottom;
            newPanelSize.Width += _padding.Right;

            // See if the stack panel might have changed size (based on content)
            //
            if (!_frameProps.WidthSpecified || !_frameProps.HeightSpecified)
            {
                SizeF panelSize = this.Frame.Size;
                if (!_frameProps.HeightSpecified)
                {
                    panelSize.Height = newPanelSize.Height;
                }
                if (!_frameProps.WidthSpecified)
                {
                    panelSize.Width = newPanelSize.Width;
                }

                // Only re-size and request superview layout if the size actually changes
                //
                if ((panelSize.Width != this.Frame.Width) || (panelSize.Height != this.Frame.Height))
                {                    
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
    }

    class iOSStackPanelWrapper : iOSControlWrapper
    {
        public iOSStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stack panel element");

            StackPanelView stackPanel = new StackPanelView(this);
            this._control = stackPanel;

            stackPanel.FrameProperties = processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(stackPanel);

            processElementProperty((string)controlSpec["orientation"], value => stackPanel.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

            processElementProperty((string)controlSpec["alignContentH"], value => stackPanel.HorizontalAlignment = ToHorizontalAlignment(value, HorizontalAlignment.Left), HorizontalAlignment.Left);
            processElementProperty((string)controlSpec["alignContentV"], value => stackPanel.VerticalAlignment = ToVerticalAlignment(value, VerticalAlignment.Center), VerticalAlignment.Center);
            processThicknessProperty(controlSpec["padding"], new PaddedViewThicknessSetter(stackPanel));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    stackPanel.AddSubview(childControlWrapper.Control);
                });
            }

            stackPanel.LayoutSubviews();
        }
    }
}