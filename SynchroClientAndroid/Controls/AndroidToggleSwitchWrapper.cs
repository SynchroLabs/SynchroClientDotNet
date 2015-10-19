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
using SynchroCore;
using System.Threading.Tasks;

namespace SynchroClientAndroid.Controls
{
    class AndroidToggleSwitchWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidToggleSwitchWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle.Attribute };

        public AndroidToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating toggle switch");

            Switch toggleSwitch = new Switch(((AndroidControlWrapper)parent).Control.Context);
            this._control = toggleSwitch;

            applyFrameworkElementDefaults(toggleSwitch);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new SynchroCore.JValue(toggleSwitch.Checked); }, value => toggleSwitch.Checked = ToBoolean(value)))
            {
                processElementProperty(controlSpec["value"], value => toggleSwitch.Checked = ToBoolean(value));
            }

            processElementProperty(controlSpec["caption"], value => toggleSwitch.Text = ToString(value));
            processElementProperty(controlSpec["onLabel"], value => toggleSwitch.TextOn = ToString(value));
            processElementProperty(controlSpec["offLabel"], value => toggleSwitch.TextOff = ToString(value));

            // Since the Toggled handler both updates the view model (locally) and may potentially have a command associated, 
            // we have to add handler in all cases (even when there is no command).
            //
            toggleSwitch.CheckedChange += toggleSwitch_CheckedChange;
        }

        async void toggleSwitch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
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