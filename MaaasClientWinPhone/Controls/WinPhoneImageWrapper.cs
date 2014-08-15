using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneImageWrapper : WinPhoneControlWrapper
    {
        public WinPhoneImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating image element");
            Image image = new Image();
            this._control = image;

            // !!! Image scaling
            //
            // image.Stretch = Stretch.Fill;          // Stretch to fill 
            // image.Stretch = Stretch.Uniform;       // Fit preserving aspect
            // image.Stretch = Stretch.UniformToFill; // Fill preserving aspect

            applyFrameworkElementDefaults(image);
            image.Height = 128; // Sizes will be overriden by the generic height/width property handlers, but
            image.Width = 128;  // we have to set these here (as defaults) in case the sizes aren't specified. 
            processElementProperty((string)controlSpec["resource"], value =>
            {
                if (value == null)
                {
                    image.Source = null;
                }
                else
                {
                    image.Source = new BitmapImage(new Uri(ToString(value)));
                }
            });
        }
    }
}