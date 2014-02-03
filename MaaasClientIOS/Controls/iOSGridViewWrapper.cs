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
    class iOSGridViewWrapper : iOSControlWrapper
    {
        public iOSGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating grid view element");

            UICollectionViewFlowLayout gridViewLaout = new UICollectionViewFlowLayout();
            UICollectionView gridView = new UICollectionView(new RectangleF(), gridViewLaout);
            this._control = gridView;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(gridView);

            // !!! TODO
        }
    }
}