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
    interface MaaasUIView
    {
        iOSControlWrapper GetControlWrapper();
    }

    class MaaasButton : UIButton, MaaasUIView
    {
        iOSButtonWrapper _wrapper;

        public MaaasButton(iOSButtonWrapper wrapper, UIButtonType type) : base(type)
        {
            _wrapper = wrapper;
        }

        public iOSControlWrapper GetControlWrapper()
        {
            return _wrapper;
        }
    }

    class iOSButtonWrapper : iOSControlWrapper
    {
        public iOSButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating button element with caption of: " + controlSpec["caption"]);

            //MaaasButton maaasButton = new MaaasButton(this, UIButtonType.RoundedRect);

            UIButton button = UIButton.FromType(UIButtonType.RoundedRect);
            this._control = button;

            processElementDimensions(controlSpec, 150, 50);
            button.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleBottomMargin;

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