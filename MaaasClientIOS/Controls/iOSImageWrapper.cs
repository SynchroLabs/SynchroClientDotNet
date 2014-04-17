using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AFNetworking;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
//using System.Drawing;
//using MonoTouch.CoreGraphics;

namespace MaaasClientIOS.Controls
{
    class iOSImageWrapper : iOSControlWrapper
    {
        private static NSUrl createNSUrl(Uri uri)
        {
            return new NSUrl(uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        }

        public iOSImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating image element");

            UIImageView image = new UIImageView();            
            this._control = image;

            // !!! Image scaling
            //
            // image.ContentMode = UIViewContentMode.ScaleToFill;     // Stretch to fill 
            // image.ContentMode = UIViewContentMode.ScaleAspectFit;  // Fit preserving aspect
            // image.ContentMode = UIViewContentMode.ScaleAspectFill; // Fill preserving aspect

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(image);
            processElementProperty((string)controlSpec["resource"], value => image.SetImageUrl(createNSUrl(this.StateManager.buildUri(ToString(value)))));
        }
    }
}