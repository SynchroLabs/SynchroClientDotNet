using SynchroCore;
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
        static Logger logger = Logger.GetLogger("WinGridViewWrapper");

        public WinGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            GridView gridView = new GridView();
            this._control = gridView;

            applyFrameworkElementDefaults(gridView);

            // !!! TODO - Implement Windows Grid View
        }
    }
}