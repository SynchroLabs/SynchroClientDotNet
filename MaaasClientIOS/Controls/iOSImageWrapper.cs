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
        static Logger logger = Logger.GetLogger("iOSImageWrapper");

        private static NSUrl createNSUrl(Uri uri)
        {
            return new NSUrl(uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        }

        public iOSImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating image element");

            UIImageView image = new UIImageView();            
            this._control = image;

            // !!! Image scaling
            //
            // image.ContentMode = UIViewContentMode.ScaleToFill;     // Stretch to fill 
            // image.ContentMode = UIViewContentMode.ScaleAspectFit;  // Fit preserving aspect
            // image.ContentMode = UIViewContentMode.ScaleAspectFill; // Fill preserving aspect

            processElementDimensions(controlSpec, 128, 128);
            applyFrameworkElementDefaults(image);
            processElementProperty((string)controlSpec["resource"], value =>
            {
                if (value == null)
                {
                    image.Image = null;
                }
                else
                {
                    image.SetImageUrl(createNSUrl(new Uri(ToString(value))));
                }
            });
        }
    }
}