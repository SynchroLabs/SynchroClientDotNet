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
    class AndroidActionWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidActionWrapper");

        static string[] Commands = new string[] { CommandName.OnClick.Attribute };

        public AndroidActionWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating action bar item with title of: {0}", controlSpec["text"]);

            this._isVisualElement = false;

            AndroidActionBarItem actionBarItem = _pageView.CreateAndAddActionBarItem();

            processElementProperty(controlSpec["text"], value => actionBarItem.Title = ToString(value));
            processElementProperty(controlSpec["icon"], value => actionBarItem.Icon = ToString(value));
            processElementProperty(controlSpec["enabled"], value => actionBarItem.IsEnabled = ToBoolean(value));

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

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                actionBarItem.OnItemSelected = this.onItemSelected;
            }
        }

        public async void onItemSelected()
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}