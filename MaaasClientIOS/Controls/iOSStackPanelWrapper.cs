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
    // "Star space" is the term used to refer to the allocation of "extra" space based on star heights
    // or widths (for example, a width of "2*" means the element wants a 2x pro-rata allocation of
    // any extra space available).
    //
    // By managing the total available star space, and returning to each caller their proportion of the
    // remaining unallocated space, we will ensure that the final consumer will get the remainder
    // of the total space (this mitigates rounding errors to some extent by at least guaranteeing
    // that the total space usage is correct).
    //
    public class StarSpaceManager
    {
        protected int _totalStars = 0;
        protected float _totalStarSpace = 0.0f;

        public StarSpaceManager(int totalStars, float totalStarSpace)
        {
            _totalStars = totalStars;
            _totalStarSpace = totalStarSpace;
        }

        public float GetStarSpace(int numStars)
        {
            float starSpace = 0.0f;
            if ((_totalStarSpace > 0) && (_totalStars > 0))
            {
                starSpace = (_totalStarSpace/_totalStars) * numStars;
                _totalStars -= numStars;
                _totalStarSpace -= starSpace;
            }
            return starSpace;
        }
    }

    class StackPanelView : PaddedView
    {
        static Logger logger = Logger.GetLogger("StackPanelView");

        protected iOSControlWrapper _controlWrapper;
        protected Orientation _orientation;
        protected float _spacing = 10;

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

        public override SizeF IntrinsicContentSize
        {
            get
            {
                // Compute the "wrap contents" (minimum) size for our contents.  This will not include
                // any allocation for star-sized children, if any (whose minimum size is implicitly zero).
                //
                SizeF intrinsicSize = new SizeF(0, 0);

                foreach (UIView childView in this.Subviews)
                {
                    if (childView.Hidden)
                    {
                        // Skip hidden children for layout purposes
                        continue;
                    }

                    iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);

                    // For FillParent ("star sized") elements, we don't want to count the current value in that dimension in
                    // the maximum or total values (those items will grow to fit when we arrange them later).
                    //
                    float countedChildHeight = (childControlWrapper.FrameProperties.StarHeight == 0) ? childView.Frame.Height : 0;
                    float countedChildWidth = (childControlWrapper.FrameProperties.StarWidth == 0) ? childView.Frame.Width : 0;

                    UIEdgeInsets margin = childControlWrapper.Margin;

                    if (_orientation == Orientation.Horizontal)
                    {
                        // Add to the width, update height as appropriate
                        intrinsicSize.Width += countedChildWidth + (margin.Left + margin.Right);
                        intrinsicSize.Height = Math.Max(intrinsicSize.Height, countedChildHeight + (margin.Top + margin.Bottom));
                    }
                    else // Orientation.Vertical
                    {
                        // Add to the height, update width as appropriate
                        intrinsicSize.Height += countedChildHeight + (margin.Top + margin.Bottom);
                        intrinsicSize.Width = Math.Max(intrinsicSize.Width, countedChildWidth + (margin.Left + margin.Right));
                    }
                }

                return intrinsicSize;
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

            if (((_controlWrapper.FrameProperties.HeightSpec == SizeSpec.FillParent) && (this.Frame.Height == 0)) ||
                ((_controlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent) && (this.Frame.Width == 0)))
            {
                // If either dimension is star sized, and the current size in that dimension is zero, then we
                // can't layout our children (we have no space to lay them out in anyway).  So this is a noop.
                //
                return;
            }

            // Determine the maximum subview size in the dimension perpendicular to the orientation, and the total
            // subview allocation in the orientation direction.
            //
            int totalStars = 0;

            SizeF contentSize = this.IntrinsicContentSize;

            foreach (UIView childView in this.Subviews)
            {
                if (childView.Hidden)
                {
                    // Skip hidden children for layout purposes
                    continue;
                }

                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);

                if (_orientation == Orientation.Horizontal)
                {
                    totalStars += childControlWrapper.FrameProperties.StarWidth;
                }
                else // Orientation.Vertical
                {
                    totalStars += childControlWrapper.FrameProperties.StarHeight;
                }
            }

            // This is how much "extra" space we have in the orientation direction
            float totalStarSpace = 0.0f;

            if (_orientation == Orientation.Horizontal)
            {
                if (_controlWrapper.FrameProperties.WidthSpec != SizeSpec.WrapContent)
                {
                    totalStarSpace = Math.Max(0, this.Frame.Width - contentSize.Width);
                }

                if (_controlWrapper.FrameProperties.HeightSpec != SizeSpec.WrapContent)
                {
                    contentSize.Height = this.Frame.Height;
                }
            }

            if (_orientation == Orientation.Vertical)
            {
                if (_controlWrapper.FrameProperties.HeightSpec != SizeSpec.WrapContent)
                {
                    totalStarSpace = Math.Max(0, this.Frame.Height - contentSize.Height);
                }

                if (_controlWrapper.FrameProperties.WidthSpec != SizeSpec.WrapContent)
                {
                    contentSize.Width = this.Frame.Width;
                }
            }

            StarSpaceManager starSpaceManager = new StarSpaceManager(totalStars, totalStarSpace);

            float _currTop = _padding.Top;
            float _currLeft = _padding.Left;

            SizeF newPanelSize = new SizeF(0, 0);

            UIEdgeInsets lastMargin = new UIEdgeInsets(0, 0, 0, 0);

            // Arrange the subviews (align as appropriate)
            //
            foreach (UIView childView in this.Subviews)
            {
                if (childView.Hidden)
                {
                    // Skip hidden children for layout purposes
                    continue;
                }

                iOSControlWrapper childControlWrapper = _controlWrapper.getChildControlWrapper(childView);
                UIEdgeInsets margin = childControlWrapper.Margin;

                RectangleF childFrame = childView.Frame;

                if (_orientation == Orientation.Horizontal)
                {
                    if (childControlWrapper.FrameProperties.StarWidth > 0)
                    {
                        childFrame.Width = starSpaceManager.GetStarSpace(childControlWrapper.FrameProperties.StarWidth);
                    }

                    // Set the horizontal position (considering margin overlap)
                    childFrame.X = _currLeft + Math.Max(lastMargin.Right, margin.Left);

                    // Set the vertical position based on aligment (default Top)
                    childFrame.Y = _currTop + margin.Top;

                    if (childControlWrapper.FrameProperties.HeightSpec == SizeSpec.FillParent)
                    {
                        // Filling to parent height (already top aligned, so set width relative to parent,
                        // accounting for margins.
                        //
                        childFrame.Height = Math.Max(0, this.Frame.Height - (margin.Top + margin.Bottom));
                    }
                    else
                    {
                        // Explicit height - align as needed.
                        //
                        if (childControlWrapper.VerticalAlignment == VerticalAlignment.Center)
                        {
                            // Should we consider margin when centering?  For now, we don't.
                            childFrame.Y = _currTop + ((contentSize.Height - childFrame.Height) / 2);
                        }
                        else if (childControlWrapper.VerticalAlignment == VerticalAlignment.Bottom)
                        {
                            childFrame.Y = _currTop + (contentSize.Height - childFrame.Height) - margin.Bottom;
                        }
                    }

                    childView.Frame = childFrame; // <== This is where we size child (the frame may or may not have actually changed)

                    // We are going to explicitly call LayoutSubviews on the child here, as opposed to using SetNeedsLayout, because we want
                    // the child to do the layout now so that we can accomodate size changes to the child (caused by its own LayoutSubviews)
                    // in our own layout logic here...
                    //
                    childView.LayoutSubviews();
                    childFrame = childView.Frame;

                    _currLeft = childFrame.X + childFrame.Width;
                }
                else // Orientation.Vertical
                {
                    if (childControlWrapper.FrameProperties.StarHeight > 0)
                    {
                        childFrame.Height = starSpaceManager.GetStarSpace(childControlWrapper.FrameProperties.StarHeight);
                    }

                    // Set the vertical position (considering margin overlap)
                    childFrame.Y = _currTop + Math.Max(lastMargin.Bottom, margin.Top);

                    // Set the horizontal position based on aligment (default Left)
                    childFrame.X = _currLeft + margin.Left;

                    if (childControlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent)
                    {
                        // Filling to parent width (already left aligned, so set width relative to parent,
                        // accounting for margins.
                        //
                        childFrame.Width = Math.Max(0, this.Frame.Width - (margin.Left + margin.Right));
                    }
                    else
                    {
                        // Explicit height - align as needed.
                        //
                        if (childControlWrapper.HorizontalAlignment == HorizontalAlignment.Center)
                        {
                            // Should we consider margin when centering?  For now, we don't.
                            childFrame.X = _currLeft + ((contentSize.Width - childFrame.Width) / 2);
                        }
                        else if (childControlWrapper.HorizontalAlignment == HorizontalAlignment.Right)
                        {
                            childFrame.X = _currLeft + (contentSize.Width - childFrame.Width) - margin.Right;
                        }
                    }

                    childView.Frame = childFrame; // <== This is where we size child (the frame may or may not have actually changed)

                    // We are going to explicitly call LayoutSubviews on the child here, as opposed to using SetNeedsLayout, because we want
                    // the child to do the layout now so that we can accomodate size changes to the child (caused by its own LayoutSubviews)
                    // in our own layout logic here...
                    //
                    childView.LayoutSubviews();
                    childFrame = childView.Frame;

                    _currTop = childFrame.Y + childFrame.Height;
                }

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
            if ((_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent) || (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent))
            {
                SizeF panelSize = this.Frame.Size;
                if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent)
                {
                    panelSize.Height = newPanelSize.Height;
                }
                if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent)
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
        static Logger logger = Logger.GetLogger("iOSStackPanelWrapper");

        public iOSStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating stack panel element");

            StackPanelView stackPanel = new StackPanelView(this);
            this._control = stackPanel;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(stackPanel, false);

            processElementProperty((string)controlSpec["orientation"], value => stackPanel.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

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