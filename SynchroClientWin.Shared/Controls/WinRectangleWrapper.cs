using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace MaaasClientWin.Controls
{
    class WinRectangleWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinRectangleWrapper");

        static string[] Commands = new string[] { CommandName.OnTap.Attribute };

        public WinRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating rectangle element");
            Rectangle rect = new Rectangle();
            this._control = rect;

            applyFrameworkElementDefaults(rect);
            processElementProperty(controlSpec, "border", value => rect.Stroke = ToBrush(value));
            processElementProperty(controlSpec, "borderThickness", value => rect.StrokeThickness = (float)ToDeviceUnits(value));
            processElementProperty(controlSpec, "cornerRadius", value => 
            {
                rect.RadiusX = (float)ToDeviceUnits(value);
                rect.RadiusY = (float)ToDeviceUnits(value);
            });
            processElementProperty(controlSpec, "fill", value => rect.Fill = ToBrush(value));
            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnTap.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnTap) != null)
            {
                rect.Tapped += rect_Tapped;
            }
        }

        async void rect_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnTap);
            if (command != null)
            {
                logger.Debug("Rectangle tapped with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
