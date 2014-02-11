using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneToggleSwitchWrapper : WinPhoneControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnToggle };

        public WinPhoneToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating toggle element with caption of: " + controlSpec["caption"]);
            ToggleSwitch toggleSwitch = new ToggleSwitch();
            this._control = toggleSwitch;

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return toggleSwitch.IsChecked; }, value => toggleSwitch.IsChecked = ToBoolean(value)))
            {
                processElementProperty((string)controlSpec["value"], value => toggleSwitch.IsChecked = ToBoolean(value));
            }

            processElementProperty((string)controlSpec["header"], value => toggleSwitch.Header = ToString(value));
            // !!! processElementProperty((string)controlSpec["onLabel"], value => toggleSwitch.OnContent = ToString(value));
            // !!! processElementProperty((string)controlSpec["offLabel"], value => toggleSwitch.OffContent = ToString(value));

            // Since the Toggled handler both updates the view model (locally) and may potentially have a command associated, 
            // we have to add handler in all cases (even when there is no command).
            //
            toggleSwitch.Checked += toggleSwitch_Checked;
            toggleSwitch.Unchecked += toggleSwitch_Unchecked;
        }

        void toggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            toggleSwitch_Toggled(sender, e);
        }

        void toggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            toggleSwitch_Toggled(sender, e);
        }

        void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                Util.debug("ToggleSwitch toggled with command: " + command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}