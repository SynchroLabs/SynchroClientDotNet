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
    class iOSToolBarWrapper : iOSControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnClick };

        public iOSToolBarWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating tool bar button element with text of: " + controlSpec["text"]);

            UIBarButtonItem buttonItem = new UIBarButtonItem("", UIBarButtonItemStyle.Plain, buttonItem_Clicked);
            processElementProperty((string)controlSpec["text"], value => buttonItem.Title = ToString(value));
            processElementProperty((string)controlSpec["enabled"], value => buttonItem.Enabled = ToBoolean(value));

            if ((string)controlSpec["control"] == "navBar.button")
            {
                _pageView.SetNavBarButton(buttonItem);
            }
            else // toolBar.button
            {
                _pageView.AddToolbarButton(buttonItem);
            }

            _isVisualElement = false;

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);
        }

        void buttonItem_Clicked(object sender, EventArgs e)
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