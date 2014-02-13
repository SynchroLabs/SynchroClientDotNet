using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    // http://code.msdn.microsoft.com/wpapps/PhotoHub-Windows-Phone-8-fd7a1093
    //
    class WinPhoneGridViewWrapper : WinPhoneControlWrapper
    {
        public WinPhoneGridViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating grid view element");
            LongListSelector gridView = new LongListSelector();
            this._control = gridView;

            // Note: Supports veritcal orientation/scroll only (probably the way you'd typically want it to work on the phone anyway)

            gridView.IsGroupingEnabled = false;
            gridView.LayoutMode = LongListSelectorLayoutMode.Grid;
            gridView.GridCellSize = new Size(100, 100); // Must be set with Grid layout

            applyFrameworkElementDefaults(gridView);

            // !!! TODO - Implement Win Phone grid view
        }
    }
}