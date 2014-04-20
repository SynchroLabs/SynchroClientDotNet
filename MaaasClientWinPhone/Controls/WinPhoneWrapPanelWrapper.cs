using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    // This Wrap panel code was inspired by this:
    //
    //    https://winrtxamltoolkit.codeplex.com/SourceControl/latest#WinRTXamlToolkit/Controls/WrapPanel/WrapPanel.cs
    //

    // Numeric utility methods used by controls.  These methods are similar in scope to the WPF DoubleUtil class.
    //
    internal static class NumericExtensions
    {
        public static bool AreClose(double left, double right)
        {
            if (left == right)
            {
                return true;
            }

            double a = (Math.Abs(left) + Math.Abs(right) + 10.0) * 2.2204460492503131E-16;
            double b = left - right;
            return (-a < b) && (a > b);
        }

        public static bool IsGreaterThan(double left, double right)
        {
            return (left > right) && !AreClose(left, right);
        }
    }

    // The OrientedSize structure is used to abstract the growth direction from the layout algorithms
    // of WrapPanel.  When the growth direction is oriented horizontally (ex: the next element is arranged
    // on the side of the previous element), then the Width grows directly with the placement of elements
    // and Height grows indirectly with the size of the largest element in the row.  When the orientation 
    // is reversed, so is the directional growth with respect to Width and Height.
    //
    class OrientedSize
    {
        // The orientation of the structure.
        //
        private Orientation _orientation;
        public Orientation Orientation
        {
            get { return _orientation; }
        }

        // The size dimension that grows directly with layout placement.
        //
        private double _direct;
        public double Direct
        {
            get { return _direct; }
            set { _direct = value; }
        }

        // The size dimension that grows indirectly with the maximum value of the layout row or column.
        //
        private double _indirect;
        public double Indirect
        {
            get { return _indirect; }
            set { _indirect = value; }
        }

        // Gets or sets the width of the size.
        //
        public double Width
        {
            get
            {
                return (Orientation == Orientation.Horizontal) ? Direct : Indirect;
            }
            set
            {
                if (Orientation == Orientation.Horizontal)
                {
                    Direct = value;
                }
                else
                {
                    Indirect = value;
                }
            }
        }

        // Gets or sets the height of the size.
        //
        public double Height
        {
            get
            {
                return (Orientation != Orientation.Horizontal) ? Direct : Indirect;
            }
            set
            {
                if (Orientation != Orientation.Horizontal)
                {
                    Direct = value;
                }
                else
                {
                    Indirect = value;
                }
            }
        }

        public OrientedSize(Orientation orientation) :
            this(orientation, 0.0, 0.0)
        {
        }

        // Initializes a new OrientedSize structure.
        //
        // Params:
        //     orientation - Orientation of the structure.
        //     width -  Un-oriented width of the structure.
        //     height - Un-oriented height of the structure.
        //
        public OrientedSize(Orientation orientation, double width, double height)
        {
            _orientation = orientation;

            _direct = 0.0;
            _indirect = 0.0;

            Width = width;
            Height = height;
        }
    }

    // WrapPanel - Positions child elements sequentially from left to right or top to bottom.  When
    // elements extend beyond the panel edge, elements are positioned in the next row or column.
    //
    public class WrapPanel : Panel
    {
        protected double _itemHeight = 0;
        public double ItemHeight
        {
            get { return _itemHeight; }
            set
            {
                _itemHeight = value;
                this.InvalidateMeasure();
            }
        }

        protected double _itemWidth = 0;
        public double ItemWidth
        {
            get { return _itemWidth; }
            set
            {
                _itemWidth = value;
                this.InvalidateMeasure();
            }
        }

        protected Orientation _orientation = Orientation.Horizontal;
        public Orientation Orientation
        {
            get { return _orientation; }
            set
            {
                _orientation = value;
                this.InvalidateMeasure();
            }
        }

        public WrapPanel()
        {
        }

        // Measures the child elements of a WrapPanel in anticipation  of arranging them during 
        // the ArrangeOverride() pass.
        //
        // Params:
        //
        //    constraint - The size available to child elements of the wrap panel.
        //
        // Returns: 
        //
        //    The size required by the WrapPanel and its elements.
        //
        protected override Size MeasureOverride(Size constraint)
        {
            // Variables tracking the size of the current line, the total size
            // measured so far, and the maximum size available to fill.  Note
            // that the line might represent a row or a column depending on the
            // orientation.
            Orientation o = Orientation;
            OrientedSize lineSize = new OrientedSize(o);
            OrientedSize totalSize = new OrientedSize(o);
            OrientedSize maximumSize = new OrientedSize(o, constraint.Width, constraint.Height);

            // Determine the constraints for individual items
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            bool hasFixedWidth = (ItemWidth > 0);
            bool hasFixedHeight = (ItemHeight > 0);
            Size itemSize = new Size(
                hasFixedWidth ? itemWidth : constraint.Width,
                hasFixedHeight ? itemHeight : constraint.Height);

            // Measure each of the Children
            foreach (UIElement element in Children)
            {
                // Determine the size of the element
                element.Measure(itemSize);
                OrientedSize elementSize = new OrientedSize(
                    o,
                    hasFixedWidth ? itemWidth : element.DesiredSize.Width,
                    hasFixedHeight ? itemHeight : element.DesiredSize.Height);

                // If this element falls of the edge of the line
                if (NumericExtensions.IsGreaterThan(lineSize.Direct + elementSize.Direct, maximumSize.Direct))
                {
                    // Update the total size with the direct and indirect growth
                    // for the current line
                    totalSize.Direct = Math.Max(lineSize.Direct, totalSize.Direct);
                    totalSize.Indirect += lineSize.Indirect;

                    // Move the element to a new line
                    lineSize = elementSize;

                    // If the current element is larger than the maximum size,
                    // place it on a line by itself
                    if (NumericExtensions.IsGreaterThan(elementSize.Direct, maximumSize.Direct))
                    {
                        // Update the total size for the line occupied by this
                        // single element
                        totalSize.Direct = Math.Max(elementSize.Direct, totalSize.Direct);
                        totalSize.Indirect += elementSize.Indirect;

                        // Move to a new line
                        lineSize = new OrientedSize(o);
                    }
                }
                else
                {
                    // Otherwise just add the element to the end of the line
                    lineSize.Direct += elementSize.Direct;
                    lineSize.Indirect = Math.Max(lineSize.Indirect, elementSize.Indirect);
                }
            }

            // Update the total size with the elements on the last line
            totalSize.Direct = Math.Max(lineSize.Direct, totalSize.Direct);
            totalSize.Indirect += lineSize.Indirect;

            // Return the total size required as an un-oriented quantity
            return new Size(totalSize.Width, totalSize.Height);
        }

        // Arranges and sizes the WrapPanel control and it child elements.
        //
        // Params:
        //
        //    finalSize - The area within the parent that the WrapPanel should use arrange itself and its children.
        //
        // Returns:
        //
        //    The actual size used by the WrapPanel
        //
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Variables tracking the size of the current line, and the maximum
            // size available to fill.  Note that the line might represent a row
            // or a column depending on the orientation.
            Orientation o = Orientation;
            OrientedSize lineSize = new OrientedSize(o);
            OrientedSize maximumSize = new OrientedSize(o, finalSize.Width, finalSize.Height);

            // Determine the constraints for individual items
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            bool hasFixedWidth = (ItemWidth > 0);
            bool hasFixedHeight = (ItemHeight > 0);
            double indirectOffset = 0;
            double? directDelta = (o == Orientation.Horizontal) ?
                (hasFixedWidth ? (double?)itemWidth : null) :
                (hasFixedHeight ? (double?)itemHeight : null);

            // Measure each of the Children.  We will process the elements one
            // line at a time, just like during measure, but we will wait until
            // we've completed an entire line of elements before arranging them.
            // The lineStart and lineEnd variables track the size of the
            // currently arranged line.
            UIElementCollection children = Children;
            int count = children.Count;
            int lineStart = 0;
            for (int lineEnd = 0; lineEnd < count; lineEnd++)
            {
                UIElement element = children[lineEnd];

                // Get the size of the element
                OrientedSize elementSize = new OrientedSize(
                    o,
                    hasFixedWidth ? itemWidth : element.DesiredSize.Width,
                    hasFixedHeight ? itemHeight : element.DesiredSize.Height);

                // If this element falls of the edge of the line
                if (NumericExtensions.IsGreaterThan(lineSize.Direct + elementSize.Direct, maximumSize.Direct))
                {
                    // Then we just completed a line and we should arrange it
                    ArrangeLine(lineStart, lineEnd, directDelta, indirectOffset, lineSize.Indirect);

                    // Move the current element to a new line
                    indirectOffset += lineSize.Indirect;
                    lineSize = elementSize;

                    // If the current element is larger than the maximum size
                    if (NumericExtensions.IsGreaterThan(elementSize.Direct, maximumSize.Direct))
                    {
                        // Arrange the element as a single line
                        ArrangeLine(lineEnd, ++lineEnd, directDelta, indirectOffset, elementSize.Indirect);

                        // Move to a new line
                        indirectOffset += lineSize.Indirect;
                        lineSize = new OrientedSize(o);
                    }

                    // Advance the start index to a new line after arranging
                    lineStart = lineEnd;
                }
                else
                {
                    // Otherwise just add the element to the end of the line
                    lineSize.Direct += elementSize.Direct;
                    lineSize.Indirect = Math.Max(lineSize.Indirect, elementSize.Indirect);
                }
            }

            // Arrange any elements on the last line
            if (lineStart < count)
            {
                ArrangeLine(lineStart, count, directDelta, indirectOffset, lineSize.Indirect);
            }

            return finalSize;
        }

        // Arrange a sequence of elements in a single line.
        //
        // Params:
        //
        //     lineStart - Index of the first element in the sequence to arrange.
        //
        //     lineEnd - Index of the last element in the sequence to arrange.
        //
        //     directDelta - Optional fixed growth in the primary direction.
        //
        //     indirectOffset - Offset of the line in the indirect direction.
        //
        //     indirectGrowth - Shared indirect growth of the elements on this line.
        //
        private void ArrangeLine(int lineStart, int lineEnd, double? directDelta, double indirectOffset, double indirectGrowth)
        {
            double directOffset = 0.0;

            Orientation o = Orientation;
            bool isHorizontal = o == Orientation.Horizontal;

            UIElementCollection children = Children;
            for (int index = lineStart; index < lineEnd; index++)
            {
                // Get the size of the element
                UIElement element = children[index];
                OrientedSize elementSize = new OrientedSize(o, element.DesiredSize.Width, element.DesiredSize.Height);

                // Determine if we should use the element's desired size or the fixed item width or height
                double directGrowth = directDelta != null ? directDelta.Value : elementSize.Direct;

                // Arrange the element
                Rect bounds = isHorizontal ?
                    new Rect(directOffset, indirectOffset, directGrowth, indirectGrowth) :
                    new Rect(indirectOffset, directOffset, indirectGrowth, directGrowth);
                element.Arrange(bounds);

                directOffset += directGrowth;
            }
        }
    }

    class WinPhoneWrapPanelWrapper : WinPhoneControlWrapper
    {
        Border _border;
        WrapPanel _panel;

        public WinPhoneWrapPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating wrappanel element");

            // In order to get padding support, we put a Border around the StackPanel...
            //
            _border = new Border();
            _panel = new WrapPanel();
            _border.Child = _panel;
            this._control = _border;

            applyFrameworkElementDefaults(_border);

            processElementProperty((string)controlSpec["orientation"], value => _panel.Orientation = ToOrientation(value, Orientation.Horizontal));
            processElementProperty((string)controlSpec["itemHeight"], value => _panel.ItemHeight = ToDeviceUnits(value));
            processElementProperty((string)controlSpec["itemWidth"], value => _panel.ItemWidth = ToDeviceUnits(value));

            processThicknessProperty(controlSpec["padding"], value => _border.Padding = (Thickness)value);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    _panel.Children.Add(childControlWrapper.Control);
                });
            }
        }
    }
}