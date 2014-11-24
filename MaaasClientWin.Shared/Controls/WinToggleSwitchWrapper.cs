using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinToggleSwitchWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinToggleSwitchWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle };

        public WinToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating toggle element with caption of: " + controlSpec["caption"]);
            ToggleSwitch toggleSwitch = new ToggleSwitch();
            this._control = toggleSwitch;

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return toggleSwitch.IsOn; }, value => toggleSwitch.IsOn = ToBoolean(value)))
            {
                processElementProperty(controlSpec["value"], value => toggleSwitch.IsOn = ToBoolean(value));
            }

            processElementProperty(controlSpec["header"], value => toggleSwitch.Header = ToString(value));
            processElementProperty(controlSpec["onLabel"], value => toggleSwitch.OnContent = ToString(value));
            processElementProperty(controlSpec["offLabel"], value => toggleSwitch.OffContent = ToString(value));

            // Since the Toggled handler both updates the view model (locally) and may potentially have a command associated, 
            // we have to add handler in all cases (even when there is no command).
            //
            toggleSwitch.Toggled += toggleSwitch_Toggled;
        }

        async void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                logger.Debug("ToggleSwitch toggled with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}