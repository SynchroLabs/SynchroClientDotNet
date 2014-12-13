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
using System.Threading.Tasks;

namespace SynchroClientAndroid.Controls
{
    class AndroidButtonWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnClick.Attribute };

        public AndroidButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            Button button = new Button(((AndroidControlWrapper)parent).Control.Context);
            this._control = button;

            applyFrameworkElementDefaults(button);

            processElementProperty(controlSpec["caption"], value => button.Text = ToString(value));

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                button.Click += button_Click;
            }
        }

        async void button_Click(object sender, EventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                logger.Debug("Button click with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}