using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SynchroCore;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    // http://docs.xamarin.com/guides/ios/user_interface/introduction_to_collection_views/
    //
    // http://forums.xamarin.com/discussion/11274/how-to-setup-uicollectionview-datasource
    //
    // https://github.com/xamarin/monotouch-samples/tree/master/SimpleCollectionView/SimpleCollectionView
    //

    public class WrapPanelCell : UICollectionViewCell
    {
        public static NSString CellID = new NSString("WrapPanelCell");

        [Export("initWithFrame:")]
        public WrapPanelCell(System.Drawing.RectangleF frame)
            : base(frame)
        {
            // BackgroundView = new UIView { BackgroundColor = UIColor.Orange };
            // SelectedBackgroundView = new UIView { BackgroundColor = UIColor.Green };
            //
            // ContentView.Layer.BorderColor = UIColor.LightGray.CGColor;
            // ContentView.Layer.BorderWidth = 2.0f;
            // ContentView.BackgroundColor = UIColor.White;
        }

        public void UpdateView(iOSControlWrapper controlWrapper)
        {
            if (ContentView.Subviews.Length > 0)
            {
                ContentView.Subviews[0].RemoveFromSuperview();
            }
            ContentView.AddSubview(controlWrapper.Control);
        }
    }

    public class WrapPanelCollectionViewSource : UICollectionViewSource
    {
        static Logger logger = Logger.GetLogger("WrapPanelCollectionViewSource");

        public WrapPanelCollectionViewSource()
        {
            ControlWrappers = new List<iOSControlWrapper>();
        }

        public List<iOSControlWrapper> ControlWrappers { get; private set; }

        protected float _itemHeight = 0;
        public float ItemHeight { get { return _itemHeight; } set { _itemHeight = value; } }

        protected float _itemWidth = 0;
        public float ItemWidth { get { return _itemWidth; } set { _itemWidth = value; } }

        public override Int32 NumberOfSections(UICollectionView collectionView)
        {
            return 1;
        }

        public override Int32 GetItemsCount(UICollectionView collectionView, Int32 section)
        {
            return ControlWrappers.Count;
        }

        public override Boolean ShouldHighlightItem(UICollectionView collectionView, NSIndexPath indexPath)
        {
            return false;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = (WrapPanelCell)collectionView.DequeueReusableCell(WrapPanelCell.CellID, indexPath);

            // logger.Info("Updating cell {0} with frame: {1}", indexPath.Item, cell.Frame);

            iOSControlWrapper controlWrapper = ControlWrappers[indexPath.Row];
            cell.UpdateView(controlWrapper);

            return cell;
        }

        // Not sure I exactly understand the magic of this particular trainwrec, but see:
        //
        //    http://forums.xamarin.com/discussion/531/how-can-i-provide-uicollectionviewdelegate-getsizeforitem-in-uicollectionviewcontroller
        //    https://bugzilla.xamarin.com/show_bug.cgi?id=8716
        //
        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public virtual SizeF SizeForItemAtIndexPath(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            return this.SizeForItemAtIndexPath(indexPath);
        }

        public SizeF SizeForItemAtIndexPath(NSIndexPath indexPath)
        {
            iOSControlWrapper controlWrapper = ControlWrappers[indexPath.Row];
            SizeF controlSize = controlWrapper.Control.Frame.Size;
            if (_itemHeight > 0)
            {
                controlSize.Height = _itemHeight;
            }
            else
            {
                controlSize.Height += controlWrapper.MarginTop + controlWrapper.MarginBottom;
            }
            if (_itemWidth > 0)
            {
                controlSize.Width = _itemWidth;
            }
            else
            {
                controlSize.Width += controlWrapper.MarginLeft + controlWrapper.MarginRight;
            }
            return controlSize;
        }

        public iOSControlWrapper ItemAtIndexPath(NSIndexPath indexPath)
        {
            return ControlWrappers[indexPath.Row];
        }
    }

    public class WrapPanelCollectionViewLayout : UICollectionViewFlowLayout
    {
        static Logger logger = Logger.GetLogger("WrapPanelCollectionViewLayout");

        protected WrapPanelCollectionViewSource _source;

        public WrapPanelCollectionViewLayout(WrapPanelCollectionViewSource source) : base()
        {
            _source = source;
        }

        public float ItemHeight
        {
            get { return _source.ItemHeight; }
            set
            {
                _source.ItemHeight = value;
                this.InvalidateLayout();
            }
        }

        public float ItemWidth
        {
            get { return _source.ItemWidth; }
            set
            {
                _source.ItemWidth = value;
                this.InvalidateLayout();
            }
        }

        protected void positionLineElements(List<UICollectionViewLayoutAttributes> lineContents, float linePosition, float lineThickness)
        {
            logger.Debug("Positioning line with {0} elements, position: {1}, thickness: {2}", lineContents.Count, linePosition, lineThickness);
            float lineLength = 0;

            foreach (UICollectionViewLayoutAttributes lineMember in lineContents)
            {
                iOSControlWrapper controlWrapper = _source.ItemAtIndexPath(lineMember.IndexPath);
                SizeF allocatedSize = _source.SizeForItemAtIndexPath(lineMember.IndexPath);
                SizeF actualSize = controlWrapper.Control.Frame.Size;

                float X, Y;

                if (this.ScrollDirection == UICollectionViewScrollDirection.Vertical)
                {
                    // Vertical scroll means horizontal layout...
                    //
                    if (controlWrapper.HorizontalAlignment == HorizontalAlignment.Left)
                    {
                        X = lineLength + controlWrapper.MarginLeft;
                    }
                    else if (controlWrapper.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        X = lineLength + allocatedSize.Width - (actualSize.Width + controlWrapper.MarginRight);
                    }
                    else // HorizontalAlignment.Center - default
                    {
                        X = lineLength + ((allocatedSize.Width - actualSize.Width) / 2);
                    }
                    lineLength += allocatedSize.Width;

                    if (controlWrapper.VerticalAlignment == VerticalAlignment.Top)
                    {
                        Y = linePosition + controlWrapper.MarginTop;
                    }
                    else if (controlWrapper.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        Y = linePosition + lineThickness - (actualSize.Height + controlWrapper.MarginBottom);
                    }
                    else // VerticalAlignment.Center - default
                    {
                        Y = linePosition + ((lineThickness - actualSize.Height) / 2);
                    }
                }
                else  // UICollectionViewScrollDirection.Horizontal;
                {
                    // Horizontal scroll means vertical layout...
                    //
                    if (controlWrapper.HorizontalAlignment == HorizontalAlignment.Left)
                    {
                        X = linePosition + controlWrapper.MarginLeft;
                    }
                    else if (controlWrapper.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        X = linePosition + lineThickness - (actualSize.Width + controlWrapper.MarginRight);
                    }
                    else // HorizontalAlignment.Center - default
                    {
                        X = linePosition + ((lineThickness - actualSize.Width) / 2);
                    }

                    if (controlWrapper.VerticalAlignment == VerticalAlignment.Top)
                    {
                        Y = lineLength + controlWrapper.MarginTop;
                    }
                    else if (controlWrapper.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        Y = lineLength + allocatedSize.Height - (actualSize.Height + controlWrapper.MarginBottom);
                    }
                    else // VerticalAlignment.Center - default
                    {
                        Y = lineLength + ((allocatedSize.Height - actualSize.Height) / 2);
                    }
                    lineLength += allocatedSize.Height;
                }

                RectangleF frame = lineMember.Frame;
                frame.X = X;
                frame.Y = Y;
                lineMember.Frame = frame;
            }
        }

        // We're going to take advantage of the fact that the UICollectionViewFlowLayout will take care of organizing
        // the items into "lines" (rows/columns as appropriate).  We then just process and lay out the line elements
        // to position each element appropriately given its margins and alignment.
        //
        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(RectangleF rect)
        {
            logger.Debug("LayoutAttributesForElementsInRect: {0}", rect);
            UICollectionViewLayoutAttributes[] attributesArray = base.LayoutAttributesForElementsInRect(rect);

            List<UICollectionViewLayoutAttributes> lineContents = new List<UICollectionViewLayoutAttributes>();

            float lineLength = 0;
            float linePosition = 0;
            float lineThickness = 0;

            foreach (UICollectionViewLayoutAttributes attributes in attributesArray) 
            {
                if (attributes.RepresentedElementKind == null)
                {
                    attributes.Frame = this.LayoutAttributesForItem(attributes.IndexPath).Frame;

                    if (this.ScrollDirection == UICollectionViewScrollDirection.Vertical)
                    {
                        // Vertical scroll means horizontal layout...
                        //
                        if (attributes.Frame.X < (lineLength - 1)) // Make sure it's enough less that it's not a rounding error
                        {
                            // New line...
                            //
                            if (lineContents.Count > 0)
                            {
                                this.positionLineElements(lineContents, linePosition, lineThickness);
                                lineContents.Clear();
                            }

                            linePosition = attributes.Frame.Y;
                            lineThickness = attributes.Frame.Height; 
                        }
                        else
                        {
                            // Continuation of current line...
                            //
                            linePosition = Math.Min(linePosition, attributes.Frame.Y);
                            lineThickness = Math.Max(lineThickness, attributes.Frame.Height);
                        }

                        lineLength = attributes.Frame.X + attributes.Frame.Width;
                    }
                    else  // UICollectionViewScrollDirection.Horizontal;
                    {
                        // Horizontal scroll means vertical layout...
                        //
                        if (attributes.Frame.Y < (lineLength - 1)) // Make sure it's enough less that it's not a rounding error
                        {
                            // New line...
                            //
                            if (lineContents.Count > 0)
                            {
                                this.positionLineElements(lineContents, linePosition, lineThickness);
                                lineContents.Clear();
                            }

                            linePosition = attributes.Frame.X;
                            lineThickness = attributes.Frame.Width;
                        }
                        else
                        {
                            // Continuation of current line...
                            //
                            linePosition = Math.Min(linePosition, attributes.Frame.X);
                            lineThickness = Math.Max(lineThickness, attributes.Frame.Width);
                        }

                        lineLength = attributes.Frame.Y + attributes.Frame.Height;
                    }

                    lineContents.Add(attributes);
                }
            }

            this.positionLineElements(lineContents, linePosition, lineThickness);
            lineContents.Clear();

            return attributesArray;
        }
    }

    public class PaddingThicknessSetter : ThicknessSetter
    {
        protected UICollectionViewFlowLayout _layout;

        public PaddingThicknessSetter(UICollectionViewFlowLayout layout)
        {
            _layout = layout;
        }

        public override void SetThicknessLeft(float thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Left = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessTop(float thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Top = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessRight(float thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Right = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessBottom(float thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Bottom = thickness;
            _layout.SectionInset = insets;
        }
    }

    class WrapPanelCollectionView : UICollectionView
    {
        protected iOSControlWrapper _controlWrapper;

        public WrapPanelCollectionView(iOSControlWrapper controlWrapper, UICollectionViewLayout layout)
            : base(new RectangleF(), layout)
        {
            _controlWrapper = controlWrapper;
            this.BackgroundColor = UIColor.Clear; // UICollectionView background defaults to Black
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            WrapPanelCollectionViewLayout layout = this.CollectionViewLayout as WrapPanelCollectionViewLayout;
            if (layout != null)
            {
                WrapPanelCollectionViewSource viewSource = this.Source as WrapPanelCollectionViewSource;

                SizeF frameSize = this.Frame.Size;

                if (layout.ScrollDirection == UICollectionViewScrollDirection.Horizontal)
                {
                    // Vertical wrapping (width may vary based on contents, height must be explicit)
                    //
                    if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent)
                    {                        
                        frameSize.Width = this.ContentSize.Width;
                    }
                }
                else
                {
                    // Horizontal wrapping (height may vary based on contents, width must be explicit)
                    //
                    if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent)
                    {
                        frameSize.Height = this.ContentSize.Height;
                    }
                }

                if ((frameSize.Width != this.Frame.Width) || (frameSize.Height != this.Frame.Height))
                {
                    RectangleF frame = this.Frame;
                    frame.Size = frameSize;
                    this.Frame = frame;
                    /*
                    if (this.Superview != null)
                    {
                        this.Superview.SetNeedsLayout();
                    }
                     */
                }
            }
        }
    }

    class iOSWrapPanelWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSWrapPanelWrapper");

        public iOSWrapPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating wrap panel element");

            var source = new WrapPanelCollectionViewSource();
            WrapPanelCollectionViewLayout layout = new WrapPanelCollectionViewLayout(source);
            UICollectionView view = new WrapPanelCollectionView(this, layout);

            this._control = view;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(view);

            // We'll use item padding to space out our items
            layout.MinimumInteritemSpacing = 0;
            layout.MinimumLineSpacing = 0;

            processElementProperty(controlSpec["orientation"], value =>
            {
                Orientation orientation = ToOrientation(value, Orientation.Horizontal);
                if (orientation == Orientation.Horizontal)
                {
                    layout.ScrollDirection = UICollectionViewScrollDirection.Vertical;
                }
                else
                {
                    layout.ScrollDirection = UICollectionViewScrollDirection.Horizontal;
                }
            });

            // Need support for fixed item height/width - has implications to item positioning within fixed dimension
            //
            processElementProperty(controlSpec["itemHeight"], value => layout.ItemHeight = (float)ToDeviceUnits(value));
            processElementProperty(controlSpec["itemWidth"], value => layout.ItemWidth = (float)ToDeviceUnits(value));

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(layout));

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    source.ControlWrappers.Add(childControlWrapper);
                });
            }

            view.RegisterClassForCell(typeof(WrapPanelCell), WrapPanelCell.CellID);
            view.Source = source;

            view.LayoutSubviews();
        }
    }
}