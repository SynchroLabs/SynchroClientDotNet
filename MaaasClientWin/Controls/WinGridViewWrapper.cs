using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinGridViewWrapper : WinControlWrapper
    {
        public WinGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating grid view element");
            GridView gridView = new GridView();
            this._control = gridView;

            applyFrameworkElementDefaults(gridView);

            // !!! TODO - Implement Windows Grid View
        }
    }
}