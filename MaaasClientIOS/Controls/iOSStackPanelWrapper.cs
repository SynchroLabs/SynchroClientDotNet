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
    public enum Alignment : uint
    {
        UNDEFINED = 0,
        Center,
        Left,
        Right,
        Top,
        Bottom,
        Stretch
    }

    class StackPanelView : UIView
    {
        protected iOSControlWrapper _controlWrapper;
        protected bool _isHorizontal = true;
        protected float _padding = 0;
        protected float _spacing = 10;
        protected Alignment _alignment = Alignment.UNDEFINED;

        public StackPanelView(iOSControlWrapper controlWrapper)
            : base()
        {
            _controlWrapper = controlWrapper;
        }

        public bool Horizontal { get { return _isHorizontal; } set { _isHorizontal = value; } }

        public Alignment Alignment { get { return _alignment; } set { _alignment = value; } }

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

            // Determine the maximum subview size in each dimension (for item alignment later).
            //
            SizeF maxSubviewSize = new SizeF(0, 0);
            foreach (UIView childView in this.Subviews)
            {
                maxSubviewSize.Width = Math.Max(maxSubviewSize.Width, childView.Frame.Width);
                maxSubviewSize.Height = Math.Max(maxSubviewSize.Height, childView.Frame.Height);
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
                if (_isHorizontal)
                {
                    if (_alignment == Alignment.Center)
                    {
                        childFrame.Y += (maxSubviewSize.Height - childFrame.Height) / 2;
                    }
                    else if (_alignment == Alignment.Bottom)
                    {
                        childFrame.Y += (maxSubviewSize.Height - childFrame.Height);
                    }
                    _currLeft += childView.Bounds.Width + _spacing;
                }
                else
                {
                    if (_alignment == Alignment.Center)
                    {
                        childFrame.X += (maxSubviewSize.Width - childFrame.Width) / 2;
                    }
                    else if (_alignment == Alignment.Right)
                    {
                        childFrame.X += (maxSubviewSize.Width - childFrame.Width);
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
            // !!! Optimize this to only re-size/notify if the size actually changes
            //
            newPanelSize.Height += _padding;
            newPanelSize.Width += _padding;

            RectangleF panelFrame = this.Frame;
            panelFrame.Size = newPanelSize;
            this.Frame = panelFrame;
            if (this.Superview != null)
            {
                this.Superview.SetNeedsLayout();
            }

            base.LayoutSubviews();
        }
    }

    class iOSStackPanelWrapper : iOSControlWrapper
    {
        public static Alignment ToAlignment(object alignmentValue, Alignment defaultAlignment = Alignment.UNDEFINED)
        {
            string alignmentString = ToString(alignmentValue);
            Alignment alignment = (Alignment)Enum.Parse(typeof(Alignment), alignmentString);
            if (Enum.IsDefined(typeof(Alignment), alignmentString))
            {
                return alignment;
            }

            return defaultAlignment;
        }

        public iOSStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stack panel element");

            StackPanelView stackPanel = new StackPanelView(this);
            this._control = stackPanel;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(stackPanel);

            processElementProperty((string)controlSpec["alignContent"], value => stackPanel.Alignment = ToAlignment(value));

            stackPanel.Horizontal = true;
            if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
            {
                stackPanel.Horizontal = false;
            }

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    stackPanel.AddSubview(childControlWrapper.Control);
                });
            }
        }
    }
}