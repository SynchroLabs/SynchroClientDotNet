using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneButtonWrapper : WinPhoneControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPhoneTextButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnClick };

        public WinPhoneButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);

            Button button = new Button();
            this._control = button;

            button.Height = 75; // !!!

            applyFrameworkElementDefaults(button);

            processElementProperty((string)controlSpec["caption"], value => button.Content = ToString(value));

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
                logger.Debug("Button click with command: {0}", command);
                this.StateManager.processCommand(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
