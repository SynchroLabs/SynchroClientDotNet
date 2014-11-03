using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinToggleWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinToggleWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle };

        public WinToggleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating command element with label of: {0}", controlSpec["text"]);

            AppBarToggleButton button = new AppBarToggleButton();

            this._control = button;

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return button.IsChecked; }, value => button.IsChecked = ToBoolean(value)))
            {
                processElementProperty((string)controlSpec["value"], value => button.IsChecked = ToBoolean(value));
            }

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
                    logger.Warn("Warning - command bar button icon not found for: {0}", ToString(value));
                }
            });

            if (_pageView.Page.BottomAppBar == null)
            {
                _pageView.Page.BottomAppBar = new CommandBar();
            }
            CommandBar commandBar = (CommandBar)_pageView.Page.BottomAppBar;

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

            button.Checked += button_Checked;
            button.Unchecked += button_Unchecked;
        }

        async Task onToggled()
        {
            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                logger.Debug("Toggle toggled with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }

        async void button_Checked(object sender, RoutedEventArgs e)
        {
            await onToggled();
        }

        async void button_Unchecked(object sender, RoutedEventArgs e)
        {
            await onToggled();
        }
    }
}
