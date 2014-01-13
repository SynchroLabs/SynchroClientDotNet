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
        protected bool _isHorizontal = true;
        protected float _padding = 0;
        protected float _spacing = 10;

        public StackPanelView(iOSControlWrapper controlWrapper)
            : base()
        {
            _controlWrapper = controlWrapper;
        }

        public bool Horizontal { get { return _isHorizontal; } set { _isHorizontal = value; } }

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
            // !!! What we really need to do here is measure all the subviews to determinal the final size of the
            //     stackpanel after layout.  Then as we position each subview, we use the alignment to determine how to
            //     position the item within the space available.
            //
            // !!! How do we access the subview configuration?  Via the wrapper somehow?

            // Util.debug("StackPanelView - Layout subviews");

            float _currTop = _padding;
            float _currLeft = _padding;

            SizeF newPanelSize = new SizeF(0, 0);

            // Arrange the subviews
            //
            foreach (UIView childView in this.Subviews)
            {
                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);

                RectangleF childFrame = childView.Frame;
                childFrame.X = _currLeft;
                childFrame.Y = _currTop;
                if (_isHorizontal)
                {
                    _currLeft += childView.Bounds.Width + _spacing;
                }
                else
                {
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
        public iOSStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stack panel element");

            StackPanelView stackPanel = new StackPanelView(this);
            this._control = stackPanel;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(stackPanel);

            processElementProperty((string)controlSpec["padding"], value => stackPanel.Padding = (float)ToDeviceUnits(value)); // !!! Simple value only for now

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