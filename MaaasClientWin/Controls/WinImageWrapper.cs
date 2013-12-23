using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MaaasClientWin.Controls
{
    class WinImageWrapper : WinControlWrapper
    {
        public WinImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating image element");
            Image image = new Image();
            this._control = image;

            applyFrameworkElementDefaults(image);
            image.Height = 128; // Sizes will be overriden by the generic height/width property handlers, but
            image.Width = 128;  // we have to set these here (as defaults) in case the sizes aren't specified. 
            processElementProperty((string)controlSpec["resource"], value => image.Source = new BitmapImage(this.StateManager.buildUri(ToString(value))));
        }
    }
}
