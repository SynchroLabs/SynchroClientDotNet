using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    class iOSButtonWrapper : iOSControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnClick };

        public iOSButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating button element with caption of: " + controlSpec["caption"]);

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

        void button_Click(object sender, EventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                Util.debug("Button click with command: " + command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}