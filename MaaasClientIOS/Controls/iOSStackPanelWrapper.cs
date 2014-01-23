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
    class StackPanelView : UIView
    {
        protected iOSControlWrapper _controlWrapper;
        protected Orientation _orientation;
        protected float _padding = 0;
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
                maxDimensions.Width = Math.Max(maxDimensions.Width, childView.Frame.Width);
                maxDimensions.Height = Math.Max(maxDimensions.Height, childView.Frame.Height);
            }

            if (_frameProps.WidthSpecified)
            {
                maxDimensions.Width = this.Frame.Width;
            }
            if (_frameProps.HeightSpecified)
            {
                maxDimensions.Height = this.Frame.Height;
            }

            float _currTop = _padding;
            float _currLeft = _padding;

            SizeF newPanelSize = new SizeF(0, 0);

            // Arrange the subviews (align as appropriate)
            //
            foreach (UIView childView in this.Subviews)
            {
                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);

                RectangleF childFrame = childView.Frame;
                childFrame.X = _currLeft;
                childFrame.Y = _currTop;
                if (_orientation == Orientation.Horizontal)
                {
                    if (_vAlign == VerticalAlignment.Center)
                    {
                        childFrame.Y += (maxDimensions.Height - childFrame.Height) / 2;
                    }
                    else if (_vAlign == VerticalAlignment.Bottom)
                    {
                        childFrame.Y += (maxDimensions.Height - childFrame.Height);
                    }
                    _currLeft += childView.Bounds.Width + _spacing;
                }
                else
                {
                    if (_hAlign == HorizontalAlignment.Center)
                    {
                        childFrame.X += (maxDimensions.Width - childFrame.Width) / 2;
                    }
                    else if (_hAlign == HorizontalAlignment.Right)
                    {
                        childFrame.X += (maxDimensions.Width - childFrame.Width);
                    }
                    _currTop += childView.Bounds.Height + _spacing;
                }
                childView.Frame = childFrame;

                if ((childFrame.X + childFrame.Width) > newPanelSize.Width)
                {
                    newPanelSize.Width = childFrame.X + childFrame.Width;
                }
                if ((childFrame.Y + childFrame.Height) > newPanelSize.Height)
                {
                    newPanelSize.Height = childFrame.Y + childFrame.Height;
                }
            }

            // Resize the stackpanel to contain the subview
            //
            newPanelSize.Height += _padding;
            newPanelSize.Width += _padding;

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