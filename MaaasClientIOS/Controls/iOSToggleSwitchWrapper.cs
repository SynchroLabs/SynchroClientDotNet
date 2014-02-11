using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace MaaasClientIOS.Controls
{
    class iOSToggleSwitchWrapper : iOSControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnToggle };

        public iOSToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating toggle switch element");

            UISwitch toggleSwitch = new UISwitch();
            this._control = toggleSwitch;

            processElementDimensions(controlSpec, 150, 50);

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return toggleSwitch.On; }, value => toggleSwitch.On = ToBoolean(value)))
            {
                processElementProperty((string)controlSpec["value"], value => toggleSwitch.On = ToBoolean(value));
            }

            // !!! processElementProperty((string)controlSpec["header"], value => toggleSwitch.Text = ToString(value));
            // !!! processElementProperty((string)controlSpec["onLabel"], value => toggleSwitch.TextOn = ToString(value));
            // !!! processElementProperty((string)controlSpec["offLabel"], value => toggleSwitch.TextOff = ToString(value));

            toggleSwitch.ValueChanged += toggleSwitch_ValueChanged;
        }

        void toggleSwitch_ValueChanged(object sender, EventArgs e)
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