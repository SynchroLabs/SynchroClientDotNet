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

            UICollectionViewFlowLayout layout = new UICollectionViewFlowLayout();
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