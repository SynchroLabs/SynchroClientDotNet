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
    class WinBorderWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinBorderWrapper");

        static string[] Commands = new string[] { CommandName.OnTap.Attribute };

        protected Border _border;

        public WinBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            _border = new Border();
            this._control = _border;

            applyFrameworkElementDefaults(_border);

            processElementProperty(controlSpec, "border", value => _border.BorderBrush = ToBrush(value));
            processThicknessProperty(controlSpec, "borderThickness", () => _border.BorderThickness, value => _border.BorderThickness = (Thickness)value);
            processElementProperty(controlSpec, "cornerRadius", value => _border.CornerRadius = new CornerRadius(ToDouble(value)));
            processThicknessProperty(controlSpec, "padding", () => _border.Padding, value => _border.Padding = (Thickness)value);
            // "background" color handled by base class

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    _border.Child = childControlWrapper.Control;
                });
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnTap.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnTap) != null)
            {
                _border.Tapped += _border_Tapped;
            }
        }

        private async void _border_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnTap);
            if (command != null)
            {
                logger.Debug("Border tapped with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
