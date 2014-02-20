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
    class AndroidActionWrapper : AndroidControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnClick };

        public AndroidActionWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating action bar item with title of: " + controlSpec["text"]);

            this._isVisualElement = false;

            AndroidActionBarItem actionBarItem = _pageView.CreateAndAddActionBarItem();

            processElementProperty((string)controlSpec["text"], value => actionBarItem.Title = ToString(value));
            processElementProperty((string)controlSpec["icon"], value => actionBarItem.Icon = ToString(value));
            processElementProperty((string)controlSpec["enabled"], value => actionBarItem.IsEnabled = ToBoolean(value));

            actionBarItem.ShowAsAction = ShowAsAction.Never;
            if (controlSpec["showAsAction"] != null)
            {
                if ((string)controlSpec["showAsAction"] == "Always")
                {
                    actionBarItem.ShowAsAction = ShowAsAction.Always;
                }
                else if ((string)controlSpec["showAsAction"] == "IfRoom")
                {
                    actionBarItem.ShowAsAction = ShowAsAction.IfRoom;
                }
            }

            if (controlSpec["showActionAsText"] != null)
            {
                if (ToBoolean(controlSpec["showActionAsText"]))
                {
                    actionBarItem.ShowAsAction |= ShowAsAction.WithText;
                }
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                actionBarItem.OnItemSelected = this.onItemSelected;
            }
        }

        public void onItemSelected()
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}