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
    class WinButtonWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnClick };

        public WinButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            Button button = new Button();
            this._control = button;

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
