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
    // !!! https://developer.apple.com/library/ios/documentation/WindowsViews/Conceptual/CollectionViewPGforIOS/CollectionViewBasics/CollectionViewBasics.html#//apple_ref/doc/uid/TP40012334-CH2-SW1

    class iOSListBoxWrapper : iOSControlWrapper
    {
        public iOSListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating list box element");

            UICollectionViewLayout layout = new UICollectionViewLayout();
            UICollectionView listbox = new UICollectionView(new RectangleF(0, 0, 0, 0), layout);
            this._control = listbox;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(listbox);

            // !!!
        }
    }
}