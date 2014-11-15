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

            // Add margins to position
            controlWrapper.Control.Frame = new RectangleF(
                controlWrapper.Control.Frame.X + controlWrapper.MarginLeft,
                controlWrapper.Control.Frame.Y + controlWrapper.MarginTop,
                controlWrapper.Control.Frame.Width,
                controlWrapper.Control.Frame.Height
                );
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

        // Not sure I exactly understand the magic of this particular trainwreck, but see:
        //
        //    http://forums.xamarin.com/discussion/531/how-can-i-provide-uicollectionviewdelegate-getsizeforitem-in-uicollectionviewcontroller
        //    https://bugzilla.xamarin.com/show_bug.cgi?id=8716
        //
        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public virtual SizeF SizeForItemAtIndexPath(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            // Return the size for this item (plus margins)
            iOSControlWrapper controlWrapper = ControlWrappers[indexPath.Row];
            SizeF controlSize = controlWrapper.Control.Frame.Size;
            controlSize.Height += controlWrapper.MarginTop + controlWrapper.MarginBottom;
            controlSize.Width += controlWrapper.MarginLeft + controlWrapper.MarginRight;
            return controlSize;
        }
    }

    public class WrapPanelCollectionViewLayout : UICollectionViewFlowLayout
    {
        static Logger logger = Logger.GetLogger("WrapPanelCollectionViewLayout");

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(RectangleF rect)
        {
            UICollectionViewLayoutAttributes[] attributesArray = base.LayoutAttributesForElementsInRect(rect);

            foreach (UICollectionViewLayoutAttributes attributes in attributesArray) 
            {
                if (attributes.RepresentedElementKind == null) 
                {
                    attributes.Frame = this.LayoutAttributesForItem(attributes.IndexPath).Frame;
                }
            }

            return attributesArray;
        }

        // By default, the UICollectionViewFlowLayout fully justifies all "full" lines (rows/columns).  That is
        // not what we want.  We want the rows/columns to be left/top justified respectively.  That is what
        // we do below...
        //
        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            UICollectionViewLayoutAttributes attributes = base.LayoutAttributesForItem(indexPath);

            if (indexPath.Item == 0) // degenerate case 1, first item of section
                return attributes;

            NSIndexPath ipPrev = NSIndexPath.FromRowSection(indexPath.Item-1, indexPath.Section);

            RectangleF prevFrame = this.LayoutAttributesForItem(ipPrev).Frame;
            RectangleF frame = attributes.Frame;

            if (this.ScrollDirection == UICollectionViewScrollDirection.Vertical)
            {
                // Vertical scroll means horizontal layout...
                //
                float prevRight = prevFrame.X + prevFrame.Width + 0;
                if (attributes.Frame.X <= prevRight) // degenerate case 2, first item of row
                    return attributes;
                frame.X = prevRight;
            }
            else  // UICollectionViewScrollDirection.Horizontal;
            {
                // Horizontal scroll means vertical layout...
                //
                float prevBottom = prevFrame.Y + prevFrame.Height + 0;
                if (attributes.Frame.Y <= prevBottom) // degenerate case 2, first item of column
                    return attributes;
                frame.Y = prevBottom;
            }

            attributes.Frame = frame;

            return attributes;
        }
    }

    public class PaddingThicknessSetter : ThicknessSetter
    {
        protected UICollectionViewFlowLayout _layout;

        public PaddingThicknessSetter(UICollectionViewFlowLayout layout)
        {
            _layout = layout;
        }

        public override void SetThicknessLeft(int thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Left = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessTop(int thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Top = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessRight(int thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Right = thickness;
            _layout.SectionInset = insets;
        }

        public override void SetThicknessBottom(int thickness)
        {
            UIEdgeInsets insets = _layout.SectionInset;
            insets.Bottom = thickness;
            _layout.SectionInset = insets;
        }
    }

    class iOSWrapPanelWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSWrapPanelWrapper");

        public iOSWrapPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating wrap panel element");

            UICollectionViewFlowLayout layout = new WrapPanelCollectionViewLayout();
            UICollectionView view = new UICollectionView(new RectangleF(), layout);

            this._control = view;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(view);

            // We'll use item padding to space out our items
            layout.MinimumInteritemSpacing = 0;
            layout.MinimumLineSpacing = 0;

            processElementProperty((string)controlSpec["orientation"], value =>
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

            // !!! Need support for fixed item height/width - has implications to item positioning within fixed dimension
            //
            // processElementProperty((string)controlSpec["itemHeight"], value => _panel.ItemHeight = ToDeviceUnits(value));
            // processElementProperty((string)controlSpec["itemWidth"], value => _panel.ItemWidth = ToDeviceUnits(value));

            processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(layout));

            var source = new WrapPanelCollectionViewSource();
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