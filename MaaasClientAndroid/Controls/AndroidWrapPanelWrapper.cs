using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Android.Util;
using Android.Graphics;

namespace SynchroClientAndroid.Controls
{
    // Here is an implementation of Android FlowLayout:
    //
    //    https://github.com/ApmeM/android-flowlayout
    //
    // This one is by Romain Guy on Google Code:
    //
    //    https://code.google.com/p/devoxx-schedule/source/browse/devoxx-android-client/src/net/peterkuterna/android/apps/devoxxsched/ui/widget/FlowLayout.java?name=422c381967&r=422c38196733ba3c54eb44418160e248ee1aea86
    //
    // The code below came from:
    //
    //    http://slodge.blogspot.no/2013/01/an-mono-for-android-wrappanelflowlayout.html
    //
    // And that code was based on: http://forums.xamarin.com/discussion/comment/156#Comment_156:
    //
 
    public class FlowLayout : ViewGroup
    {
        static Logger logger = Logger.GetLogger("FlowLayout");

        public FlowLayout(Context context)
            : base(context)
        {
        }

        public bool DebugDraw = false;

        protected Orientation _orientation = Orientation.Horizontal;
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                this.Invalidate();
                this.RequestLayout();
            }
        }

        protected int _itemHeight = 0;
        public int ItemHeight
        {
            get { return _itemHeight; }
            set
            {
                _itemHeight = value;
                this.RequestLayout();
            }
        }

        protected int _itemWidth = 0;
        public int ItemWidth
        {
            get { return _itemWidth; }
            set
            {
                _itemWidth = value;
                this.RequestLayout();
            }
        }

        // The line elements have been position in the dimension in which the are running, but we can't position
        // them in the other dimension until we fill the line (and know the "thickness" of the line, as determined
        // by the size of the thickest element on the line).  So this function goes back and positions in the 
        // dimension opposite of the running dimension based on the line thickness, the element margins, and the
        // element alignment (gravity).
        //
        protected void positionLineElements(List<View> lineContents, int linePosition, int lineThickness)
        {
            foreach (View lineMember in lineContents)
            {
                var lp = (WrapLayoutParams)lineMember.LayoutParameters;

                if (_orientation == Orientation.Horizontal)
                {
                    if (lp.Gravity.HasFlag(GravityFlags.Top))
                    {
                        lp.Y = PaddingTop + linePosition + lp.TopMargin;
                    }
                    else if (lp.Gravity.HasFlag(GravityFlags.Bottom))
                    {
                        lp.Y = PaddingTop + linePosition + lineThickness - (lineMember.MeasuredHeight + lp.BottomMargin);
                    }
                    else // Center - default
                    {
                        lp.Y = PaddingTop + linePosition + (lineThickness - lineMember.MeasuredHeight)/2;
                    }
                }
                else
                {
                    if (lp.Gravity.HasFlag(GravityFlags.Left))
                    {
                        lp.X = PaddingLeft + linePosition + lp.LeftMargin;
                    }
                    else if (lp.Gravity.HasFlag(GravityFlags.Right))
                    {
                        lp.X = PaddingLeft + linePosition + lineThickness - (lineMember.MeasuredWidth + lp.RightMargin);
                    }
                    else // Center - default
                    {
                        lp.X = PaddingLeft + linePosition + (lineThickness - lineMember.MeasuredWidth)/2;
                    }
                }
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var sizeWidth = MeasureSpec.GetSize(widthMeasureSpec) - PaddingRight - PaddingLeft;
            var sizeHeight = MeasureSpec.GetSize(heightMeasureSpec) - PaddingRight - PaddingLeft;

            var modeWidth = (int)MeasureSpec.GetMode(widthMeasureSpec);
            var modeHeight = (int)MeasureSpec.GetMode(heightMeasureSpec);

            int size;
            int mode;

            if (_orientation == Orientation.Horizontal)
            {
                size = sizeWidth;
                mode = modeWidth;
            }
            else
            {
                size = sizeHeight;
                mode = modeHeight;
            }

            var lineThickness = 0;
            var lineLength = 0;

            var linePosition = 0;

            var controlMaxLength = 0;
            var controlMaxThickness = 0;

            List<View> lineContents = new List<View>();

            var count = ChildCount;
            for (var i = 0; i < count; i++)
            {
                var child = GetChildAt(i);
                if (child.Visibility == ViewStates.Gone)
                {
                    continue;
                }

                var lp = (WrapLayoutParams)child.LayoutParameters;

                child.Measure(
                    GetChildMeasureSpec(widthMeasureSpec, PaddingLeft + PaddingTop + lp.LeftMargin + lp.RightMargin, lp.Width),
                    GetChildMeasureSpec(heightMeasureSpec, PaddingTop + PaddingBottom + lp.TopMargin + lp.BottomMargin, lp.Height)
                    );

                var childTotalWidth = _itemWidth != 0 ? _itemWidth : child.MeasuredWidth + lp.LeftMargin + lp.RightMargin;
                var childTotalHeight = _itemHeight != 0 ? _itemHeight : child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;

                int childLength;    // Running dimension
                int childThickness; // Opposite dimension

                if (_orientation == Orientation.Horizontal)
                {
                    childLength = childTotalWidth;
                    childThickness = childTotalHeight;
                }
                else
                {
                    childLength = childTotalHeight;
                    childThickness = childTotalWidth;
                }

                if (((MeasureSpecMode)mode != MeasureSpecMode.Unspecified) && ((lineLength + childLength) > size))
                {
                    // New line...
                    //
                    this.positionLineElements(lineContents, linePosition, lineThickness);
                    lineContents.Clear();

                    linePosition = linePosition + lineThickness;
                    lineThickness = childThickness;
                    lineLength = childLength;
                }
                else
                {
                    // Continuation of current line...
                    //
                    lineThickness = Math.Max(lineThickness, childThickness);
                    lineLength += childLength;
                }

                lineContents.Add(child);

                // The positioning below is complex because of the case where there is a fixed item size and the element
                // needs to be positioning in the running dimension within that fixed size (as opposed to just stacking it
                // next to the previous element, as happens without fixed element sizes).  In the case where there is not
                // a fixed element size, the child "total" size is the same as the measured size plus margins, meaning that
                // the math below results in the same position regardless of the gravity / math used.
                //
                if (_orientation == Orientation.Horizontal)
                {
                    if (lp.Gravity.HasFlag(GravityFlags.Left))
                    {
                        lp.X = PaddingLeft + lineLength - childTotalWidth + lp.LeftMargin;
                    }
                    else if (lp.Gravity.HasFlag(GravityFlags.Right))
                    {
                        lp.X = PaddingLeft + lineLength - (child.MeasuredWidth + lp.RightMargin);
                    }
                    else // Center - default
                    {
                        lp.X = PaddingLeft + lineLength - childTotalWidth + ((childTotalWidth - child.MeasuredWidth) / 2);
                    }
                }
                else
                {
                    if (lp.Gravity.HasFlag(GravityFlags.Top))
                    {
                        lp.Y = PaddingTop + lineLength - childTotalHeight + lp.TopMargin;
                    }
                    else if (lp.Gravity.HasFlag(GravityFlags.Bottom))
                    {
                        lp.Y = PaddingTop + lineLength - (child.MeasuredHeight + lp.BottomMargin);
                    }
                    else // Center - default
                    {
                        lp.Y = PaddingTop + lineLength - childTotalHeight + ((childTotalHeight - child.MeasuredHeight) / 2);
                    }
                }

                controlMaxLength = Math.Max(controlMaxLength, lineLength);
                controlMaxThickness = linePosition + lineThickness;
            }

            this.positionLineElements(lineContents, linePosition, lineThickness);
            lineContents.Clear();

            if (_orientation == Orientation.Horizontal)
            {
                SetMeasuredDimension(ResolveSize(controlMaxLength, widthMeasureSpec), ResolveSize(controlMaxThickness, heightMeasureSpec));
            }
            else
            {
                SetMeasuredDimension(ResolveSize(controlMaxThickness, widthMeasureSpec), ResolveSize(controlMaxLength, heightMeasureSpec));
            }
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            var count = ChildCount;
            for (var i = 0; i < count; i++)
            {
                var child = GetChildAt(i);
                var lp = (WrapLayoutParams)child.LayoutParameters;
                child.Layout(lp.X, lp.Y, lp.X + child.MeasuredWidth, lp.Y + child.MeasuredHeight);
            }
        }

        protected override bool CheckLayoutParams(ViewGroup.LayoutParams p)
        {
            return p is WrapLayoutParams;
        }

        protected override ViewGroup.LayoutParams GenerateDefaultLayoutParams()
        {
            return new WrapLayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        }

        protected override ViewGroup.LayoutParams GenerateLayoutParams(ViewGroup.LayoutParams p)
        {
            if (p is LinearLayout.LayoutParams)
            {
                return new WrapLayoutParams((LinearLayout.LayoutParams)p);
            }
            else if (p is MarginLayoutParams)
            {
                return new WrapLayoutParams((MarginLayoutParams)p);
            }
            else
            {
                return new WrapLayoutParams(p);
            }
        }

        public class WrapLayoutParams : LinearLayout.LayoutParams
        {
            public int X;
            public int Y;

            public WrapLayoutParams(int width, int height) : base(width, height) { }

            public WrapLayoutParams(ViewGroup.LayoutParams layoutParams) : base(layoutParams) { }

            public WrapLayoutParams(ViewGroup.MarginLayoutParams layoutParams) : base(layoutParams) { }

            // Xamarin does't expose the copy constructor (which takes a LinearLayout.LayoutParams), so we call
            // the MarginLayoutParams base class constructor, then propagate the gravity and weight ourselves.
            //
            public WrapLayoutParams(LinearLayout.LayoutParams layoutParams) : base((ViewGroup.MarginLayoutParams)layoutParams) 
            {
                this.Gravity = layoutParams.Gravity;
                this.Weight = layoutParams.Weight;
            }

            public override string ToString()
            {
                return "WrapLayoutParams pos(" + X + "," + Y + "), gravity: " + this.Gravity;
            }

            public void SetPosition(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }

    class AndroidWrapPanelWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidWrapPanelWrapper");

        public AndroidWrapPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating wrap panel element");

            FlowLayout layout = new FlowLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = layout;

            applyFrameworkElementDefaults(layout);

            if (controlSpec["orientation"] == null)
            {
                layout.Orientation = Orientation.Vertical;
            }
            else
            {
                processElementProperty(controlSpec["orientation"], value => layout.Orientation = ToOrientation(value, Orientation.Vertical));
            }

            processElementProperty(controlSpec["itemHeight"], value => layout.ItemHeight = (int)ToDeviceUnits(value));
            processElementProperty(controlSpec["itemWidth"], value => layout.ItemWidth = (int)ToDeviceUnits(value));

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(this.Control));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    childControlWrapper.AddToLinearLayout(layout, childControlSpec);
                });
            }

            layout.ForceLayout();
        }
    }
}