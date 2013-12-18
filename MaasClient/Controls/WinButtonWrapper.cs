using MaasClient.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaasClient.Controls
{
    class WinButtonWrapper : WinControlWrapper
    {
        public WinButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating button element with caption of: " + controlSpec["caption"]);
            Button button = new Button();
            this._control = button;

            applyFrameworkElementDefaults(button);
 
            processElementProperty((string)controlSpec["caption"], value => button.Content = ToString(value));

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "onClick", new string[] { "onClick" });
            ProcessCommands(bindingSpec, new string[] { "onClick" });
            if (GetCommand("onClick") != null)
            {
                button.Click += button_Click;
            }
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            CommandInstance command = GetCommand("onClick");
            if (command != null)
            {
                Util.debug("Button click with command: " + command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
