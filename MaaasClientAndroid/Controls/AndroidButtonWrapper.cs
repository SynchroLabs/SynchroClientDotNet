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
    class AndroidButtonWrapper : AndroidControlWrapper
    {
        public AndroidButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating button element with caption of: " + controlSpec["caption"]);
            Button button = new Button(((AndroidControlWrapper)parent).Control.Context);
            this._control = button;

            applyFrameworkElementDefaults(button);

            processElementProperty((string)controlSpec["caption"], value => button.Text = ToString(value));

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "onClick", new string[] { "onClick" });
            ProcessCommands(bindingSpec, new string[] { "onClick" });
            if (GetCommand("onClick") != null)
            {
                button.Click += button_Click;
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