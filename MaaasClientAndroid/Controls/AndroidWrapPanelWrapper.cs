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
using Newtonsoft.Json.Linq;
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
        public int HorizontalSpacing = 20;  // !!!
        public int VerticalSpacing = 20;    // !!!

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

        public bool DebugDraw = false;

        public FlowLayout(Context context)
            : base(context)
        {
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

            var lineThicknessWithSpacing = 0;
            var lineThickness = 0;
            var lineLengthWithSpacing = 0;

            var prevLinePosition = 0;

            var controlMaxLength = 0;
            var controlMaxThickness = 0;

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
                    GetChildMeasureSpec(widthMeasureSpec, PaddingLeft + PaddingTop, lp.Width),
                    GetChildMeasureSpec(heightMeasureSpec, PaddingTop + PaddingBottom, lp.Height)
                    );

                var hSpacing = GetHorizontalSpacing(lp);
                var vSpacing = GetVerticalSpacing(lp);

                var childWidth = child.MeasuredWidth;
                var childHeight = child.MeasuredHeight;

                int childLength;
                int childThickness;
                int spacingLength;
                int spacingThickness;

                if (_orientation == Orientation.Horizontal)
                {
                    childLength = childWidth;
                    childThickness = childHeight;
                    spacingLength = hSpacing;
                    spacingThickness = vSpacing;
                }
                else
                {
                    childLength = childHeight;
                    childThickness = childWidth;
                    spacingLength = vSpacing;
                    spacingThickness = hSpacing;
                }

                var lineLength = lineLengthWithSpacing + childLength;
                lineLengthWithSpacing = lineLength + spacingLength;

                var newLine = lp.NewLine || ((MeasureSpecMode)mode != MeasureSpecMode.Unspecified && lineLength > size);
                if (newLine)
                {
                    prevLinePosition = prevLinePosition + lineThicknessWithSpacing;

                    lineThickness = childThickness;
                    lineLength = childLength;
                    lineThicknessWithSpacing = childThickness + spacingThickness;
                    lineLengthWithSpacing = lineLength + spacingLength;
                }

                lineThicknessWithSpacing = Math.Max(lineThicknessWithSpacing, childThickness + spacingThickness);
                lineThickness = Math.Max(lineThickness, childThickness);

                int posX;
                int posY;
                if (_orientation == Orientation.Horizontal)
                {
                    posX = PaddingLeft + lineLength - childLength;
                    posY = PaddingTop + prevLinePosition;
                }
                else
                {
                    posX = PaddingLeft + prevLinePosition;
                    posY = PaddingTop + lineLength - childHeight;
                }
                lp.SetPosition(posX, posY);

                controlMaxLength = Math.Max(controlMaxLength, lineLength);
                controlMaxThickness = prevLinePosition + lineThickness;
            }

            if (_orientation == Orientation.Horizontal)
            {
                SetMeasuredDimension(ResolveSize(controlMaxLength, widthMeasureSpec), ResolveSize(controlMaxThickness, heightMeasureSpec));
            }
            else
            {
                SetMeasuredDimension(ResolveSize(controlMaxThickness, widthMeasureSpec), ResolveSize(controlMaxLength, heightMeasureSpec));
            }
        }

        private int GetVerticalSpacing(WrapLayoutParams lp)
        {
            int vSpacing;
            if (lp.VerticalSpacingSpecified())
            {
                vSpacing = lp.VerticalSpacing;
            }
            else
            {
                vSpacing = VerticalSpacing;
            }
            return vSpacing;
        }

        private int GetHorizontalSpacing(WrapLayoutParams lp)
        {
            int hSpacing;
            if (lp.HorizontalSpacingSpecified())
            {
                hSpacing = lp.HorizontalSpacing;
            }
            else
            {
                hSpacing = HorizontalSpacing;
            }
            return hSpacing;
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
            if (p is MarginLayoutParams)
            {
                return new WrapLayoutParams((MarginLayoutParams)p);
            }
            else
            {
                return new WrapLayoutParams(p);
            }
        }

        // !!! This whole horizontal/vertical spacing notion is going away in favor of layout
        //     spacing based on the item margins.
        //
        public class WrapLayoutParams : ViewGroup.MarginLayoutParams
        {
            private const int NoSpacing = -1;

            public int X;
            public int Y;
            public int HorizontalSpacing = NoSpacing;
            public int VerticalSpacing = NoSpacing;
            public bool NewLine = false;

            public WrapLayoutParams(int width, int height) : base(width, height) { }

            public WrapLayoutParams(ViewGroup.LayoutParams layoutParams) : base(layoutParams) { }

            public WrapLayoutParams(ViewGroup.MarginLayoutParams layoutParams) : base(layoutParams) { }

            public bool HorizontalSpacingSpecified()
            {
                return HorizontalSpacing != NoSpacing;
            }

            public bool VerticalSpacingSpecified()
            {
                return VerticalSpacing != NoSpacing;
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
        public AndroidWrapPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating wrap panel element");

            FlowLayout layout = new FlowLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = layout;

            applyFrameworkElementDefaults(layout);

            processElementProperty((string)controlSpec["orientation"], value => layout.Orientation = ToOrientation(value, Orientation.Vertical), Orientation.Vertical);

            // !!! Need to support fixed size items
            //
            // processElementProperty((string)controlSpec["itemHeight"], value => _panel.ItemHeight = ToDeviceUnits(value));
            // processElementProperty((string)controlSpec["itemWidth"], value => _panel.ItemWidth = ToDeviceUnits(value));

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(this.Control));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    layout.AddView(childControlWrapper.Control);
                });
            }

            layout.ForceLayout();
        }
    }
}