using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace MaaasClientIOS.Controls
{
    class iOSButtonWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnClick };

        public iOSButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);

            UIButton button = UIButton.FromType(UIButtonType.RoundedRect);
            this._control = button;

            processElementDimensions(controlSpec);
            applyFrameworkElementDefaults(button);

            processElementProperty((string)controlSpec["caption"], value => 
            {
                button.SetTitle(ToString(value), UIControlState.Normal);
                this.SizeToFit();
            });

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                button.TouchUpInside += button_Click;
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