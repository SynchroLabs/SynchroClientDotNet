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
            Util.debug("Creating action bar item with title of: " + controlSpec["title"]);

            this._isVisualElement = false;

            AndroidActionBarItem actionBarItem = _pageView.CreateAndAddActionBarItem();

            processElementProperty((string)controlSpec["title"], value => actionBarItem.Title = ToString(value));

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