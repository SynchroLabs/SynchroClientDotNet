using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    // http://docs.xamarin.com/guides/ios/user_interface/introduction_to_collection_views/
    //
    class iOSGridViewWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSGridViewWrapper");

        public iOSGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating grid view element");

            UICollectionViewFlowLayout gridViewLaout = new UICollectionViewFlowLayout();
            UICollectionView gridView = new UICollectionView(new RectangleF(), gridViewLaout);
            this._control = gridView;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(gridView);

            // !!! TODO - iOS Grid View
        }
    }
}