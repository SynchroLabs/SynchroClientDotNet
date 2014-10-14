using MaaasCore;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasClientWinPhone.Controls
{
    // !!! Icon set in the WinPhone SDK is pretty limited.  Consider using http://metro.useicons.com/ (maybe for both Win/WinPhone)
    //
    // !!! Add ability to provide custom icon (Win/WinPhone/Android)
    //

    class WinPhoneAppBarWrapper : WinPhoneControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPhoneAppBarWrapper");

        static string[] Commands = new string[] { CommandName.OnClick };

        public WinPhoneAppBarWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating app bar item with text of: {0}", controlSpec["text"]);

            // http://msdn.microsoft.com/en-us/library/windowsphone/develop/ff431786(v=vs.105).aspx
            //
            // Both button and item implement IApplicationBarMenuItem
            //
            IApplicationBarMenuItem appBarMenuItem = null;

            if ((string)controlSpec["control"] == "appBar.button")
            {
                ApplicationBarIconButton button = new ApplicationBarIconButton();
                button.IconUri = new Uri("/Assets/Icons/add.png", UriKind.Relative);
                processElementProperty((string)controlSpec["text"], value => button.Text = ToString(value));
                button.IconUri = new Uri("/Assets/Icons/" + (string)controlSpec["icon"] + ".png", UriKind.Relative);
                _pageView.AddAppBarIconButton(button);
                appBarMenuItem = button;
            }
            else // appBar.menuItem
            {
                ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem();
                processElementProperty((string)controlSpec["text"], value => menuItem.Text = ToString(value));
                _pageView.AddAppBarMenuItem(menuItem);
                appBarMenuItem = menuItem;
            }

            _isVisualElement = false;

            processElementProperty((string)controlSpec["enabled"], value => appBarMenuItem.IsEnabled = ToBoolean(value));

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                appBarMenuItem.Click += appBarMenuItem_Click;
            }
        }

        void appBarMenuItem_Click(object sender, EventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                logger.Debug("AppBar menu item click with command: {0}", command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
