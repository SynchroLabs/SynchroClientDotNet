using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MaaasClientWin.Controls
{
    class WinButtonWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnClick.Attribute };

        public WinButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            Button button = new Button();
            this._control = button;

            applyFrameworkElementDefaults(button);

            // It's going to be a big pain to combine text and graphics (and preserve the button text styling).  A better
            // approach for anything other than a plain text button might just be to allow setting the contents to some set
            // of controls (via a "contents" attribute, like in any single item container). That would give you layout control
            // (image size/scaling, relationship to text, etc) and should work on all platforms - but you would give up platform
            // button text styling (presumably).
            //
            // Note: Setting the background to an image brush was attempted, but the background got changed (and the image
            //       disappeared) on push (WinPhone) or pointerover (Win), and that was pretty ugly.  The fix to that,
            //       overriding the entire button styling, is also ugly.  So I gave up on that (for now anyway).
            //
            processElementProperty(controlSpec["caption"], value => button.Content = ToString(value));
            processElementProperty(controlSpec["resource"], value =>
            {
                String img = ToString(value);
                if (String.IsNullOrEmpty(img))
                {
                    button.Content = null;
                }
                else
                {
                    button.Content = new Image
                    {
                        Source = new BitmapImage(new Uri(img)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            });

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
