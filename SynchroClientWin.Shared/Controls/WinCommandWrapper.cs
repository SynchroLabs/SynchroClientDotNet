using SynchroCore;
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
        static Logger logger = Logger.GetLogger("WinCommandWrapper");

        static string[] Commands = new string[] { CommandName.OnClick.Attribute };

        public WinCommandWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating command element with label of: {0}", controlSpec["text"]);

            AppBarButton button = new AppBarButton();

            this._control = button;

            processElementProperty(controlSpec, "text", value => button.Label = ToString(value));
            processElementProperty(controlSpec, "icon", value => 
            {   
                Symbol iconSymbol;
                if (Enum.TryParse(ToString(value), out iconSymbol))
                {
                    button.Icon = new SymbolIcon(iconSymbol);
                }
                else
                {
                    logger.Warn("Warning - command bar button icon not found for: {0}", ToString(value));
                }
            });

            CommandBar commandBar = null;

            if (((string)controlSpec["commandBar"]) == "Top")
            {
#if WINDOWS_APP
                if (_pageView.Page.TopAppBar == null)
                {
                    _pageView.Page.TopAppBar = new CommandBar();
                }
                commandBar = (CommandBar)_pageView.Page.TopAppBar;
#else
                logger.Error("Command bar value of Top not supported on this platform");
#endif
            }
            else if ((controlSpec["commandBar"] == null) || (((string)controlSpec["commandBar"]) == "Bottom"))
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
#if WINDOWS_PHONE_APP
                commandBar.Visibility = Visibility.Visible;
#endif
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnClick) != null)
            {
                button.Click += button_Click;
            }
        }

        async void button_Click(object sender, RoutedEventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                logger.Debug("Button click with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
