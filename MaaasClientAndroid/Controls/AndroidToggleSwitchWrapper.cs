using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace MaaasClientAndroid.Controls
{
    class AndroidToggleSwitchWrapper : AndroidControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnToggle };

        public AndroidToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating toggle switch");

            Switch toggleSwitch = new Switch(((AndroidControlWrapper)parent).Control.Context);
            this._control = toggleSwitch;

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return toggleSwitch.Checked; }, value => toggleSwitch.Checked = ToBoolean(value)))
            {
                processElementProperty((string)controlSpec["value"], value => toggleSwitch.Checked = ToBoolean(value));
            }

            processElementProperty((string)controlSpec["header"], value => toggleSwitch.Text = ToString(value));
            processElementProperty((string)controlSpec["onLabel"], value => toggleSwitch.TextOn = ToString(value));
            processElementProperty((string)controlSpec["offLabel"], value => toggleSwitch.TextOff = ToString(value));

            // Since the Toggled handler both updates the view model (locally) and may potentially have a command associated, 
            // we have to add handler in all cases (even when there is no command).
            //
            toggleSwitch.CheckedChange += toggleSwitch_CheckedChange;
        }

        void toggleSwitch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
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