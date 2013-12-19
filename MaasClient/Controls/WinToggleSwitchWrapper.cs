using MaaasCore;
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
    class WinToggleSwitchWrapper : WinControlWrapper
    {
        public WinToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating toggle element with caption of: " + controlSpec["caption"]);
            ToggleSwitch toggleSwitch = new ToggleSwitch();
            this._control = toggleSwitch;

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", new string[] { "onToggle" });
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return toggleSwitch.IsOn; }, value => toggleSwitch.IsOn = ToBoolean(value)))
            {
                processElementProperty((string)controlSpec["value"], value => toggleSwitch.IsOn = ToBoolean(value));
            }

            processElementProperty((string)controlSpec["header"], value => toggleSwitch.Header = ToString(value));
            processElementProperty((string)controlSpec["onLabel"], value => toggleSwitch.OnContent = ToString(value));
            processElementProperty((string)controlSpec["offLabel"], value => toggleSwitch.OffContent = ToString(value));

            ProcessCommands(bindingSpec, new string[] { "onToggle" });

            // Since the Toggled handler both updates the view model (locally) and may potentially have a command associated, 
            // we have to add handler in all cases (even when there is no command).
            //
            toggleSwitch.Toggled += toggleSwitch_Toggled;
        }

        void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand("onToggle");
            if (command != null)
            {
                Util.debug("ToggleSwitch toggled with command: " + command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}