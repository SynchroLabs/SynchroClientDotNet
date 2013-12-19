using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MaasClient.Controls
{
    class WinStackPanelWrapper : WinControlWrapper
    {
        public WinStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stackpanel element");
            StackPanel stackPanel = new StackPanel();
            this._control = stackPanel;

            applyFrameworkElementDefaults(stackPanel);

            Orientation orientation = Orientation.Horizontal;
            if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
            {
                orientation = Orientation.Vertical;
            }
            stackPanel.Orientation = orientation;

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    stackPanel.Children.Add(childControlWrapper.Control);
                });
            }
        }
    }
}
