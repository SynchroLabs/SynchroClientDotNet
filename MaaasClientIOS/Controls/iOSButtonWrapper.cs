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
        public iOSButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating button element with caption of: " + controlSpec["caption"]);

            UIButton button = UIButton.FromType(UIButtonType.RoundedRect);
            this._control = button;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(button);

            processElementProperty((string)controlSpec["caption"], value => button.SetTitle(ToString(value), UIControlState.Normal));

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "onClick", new string[] { "onClick" });
            ProcessCommands(bindingSpec, new string[] { "onClick" });
            if (GetCommand("onClick") != null)
            {
                button.TouchUpInside += button_Click;
            }
        }

        void button_Click(object sender, EventArgs e)
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