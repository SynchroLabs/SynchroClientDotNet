using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    // http://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.symbol.aspx
    //

    class WinCommandWrapper : WinControlWrapper
    {
        static string[] Commands = new string[] { CommandName.OnClick };

        public WinCommandWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating command element with label of: " + controlSpec["text"]);

            AppBarButton button = new AppBarButton();
            this._control = button;

            processElementProperty((string)controlSpec["text"], value => button.Label = ToString(value));
            processElementProperty((string)controlSpec["icon"], value => 
            {   
                Symbol iconSymbol;
                if (Enum.TryParse(ToString(value), out iconSymbol))
                {
                    button.Icon = new SymbolIcon(iconSymbol);
                }
                else
                {
                    Util.debug("Warning - command bar button icon not found for: " + ToString(value));
                }
            });

            CommandBar commandBar = null;

            if (((string)controlSpec["commandBar"]) == "Top")
            {
                if (_pageView.Page.TopAppBar == null)
                {
                    _pageView.Page.TopAppBar = new CommandBar();
                }
                commandBar = (CommandBar)_pageView.Page.TopAppBar;
            }
            else if (((string)controlSpec["commandBar"]) == "Bottom")
            {
                if (_pageView.Page.BottomAppBar == null)
                {
                    _pageView.Page.BottomAppBar = new CommandBar();
                }
                commandBar = (CommandBar)_pageView.Page.BottomAppBar;
            }

            if (commandBar != null)
            {
                this._isVisualElement = false;
                if (((string)controlSpec["commandType"]) != "Secondary")
                {
                    commandBar.PrimaryCommands.Add(button);
                }
                else
                {
                    commandBar.SecondaryCommands.Add(button);
                }
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                button.Click += button_Click;
            }
        }

        void button_Click(object sender, RoutedEventArgs e)
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
