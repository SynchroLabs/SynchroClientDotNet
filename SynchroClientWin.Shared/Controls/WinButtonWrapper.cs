using SynchroClientWin;
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

        // The Glyph in the Material font sits at the top of the box, such that it needs to be
        // padded down to align with adjacent text.  This factor brings the glyph baseline down to
        // the text baseline (at least with standard fonts).
        //
        static double IconTopPadFactor = 0.2;

        static double IconTextSpacing = 5; 

        TextBlock _icon;
        TextBlock _text;

        public WinButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating button element with caption of: {0}", controlSpec["caption"]);
            Button button = new Button();
            this._control = button;

            applyFrameworkElementDefaults(button);

            var container = new StackPanel();
            container.Orientation = Orientation.Horizontal;
            container.VerticalAlignment = VerticalAlignment.Center;
            button.Content = container;

            // The text control needs to be added whether or not there is any text.  Since the Material Design icon
            // font doesn't have anything below the baseline, we need the text control so that auto-height controls
            // will be the correct size on WinPhone when they have an icon only.
            //
            _text = new TextBlock();
            container.Children.Add(_text);

            button.Loaded += Button_Loaded;

            if (ToBoolean(controlSpec["borderless"], false))
            {
                button.BorderThickness = new Thickness(0);
                button.Background = null;
            }

            processElementProperty(controlSpec, "caption", value =>
            {
                _text.Text = ToString(value);
                adjustIconPadding(button);
            });

            processElementProperty(controlSpec, "icon", value =>
            {
                if (_icon == null)
                {
                    _icon = new TextBlock();
                    _icon.FontFamily = GlyphMapper.getFontFamily(); // Will trigger adjustIconPadding() on setting font family
                    container.Children.Insert(0, _icon);
                }
                _icon.Text = GlyphMapper.getGlyph(ToString(value));
            });

            // Note: Setting the background to an image brush was attempted, but the background got changed (and the image
            //       disappeared) on push (WinPhone) or pointerover (Win), and that was pretty ugly.  The fix to that,
            //       overriding the entire button styling, is also ugly.  So I gave up on that (for now anyway).
            //
            // Also: The "resource" content option is mutually exclusive to the icon/caption option, by design.
            //
            processElementProperty(controlSpec, "resource", value =>
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

        protected void adjustIconPadding(Button button)
        {
            // The Glyph in the Material font needs to be padded down to align with adjacent text.  
            // We also add padding between the icon and text if there is text currently set.
            //
            if (_icon != null)
            {
                var iconTopPad = button.FontSize * IconTopPadFactor;
                _icon.Padding = new Thickness(0, iconTopPad, (_text.Text.Length > 0) ? IconTextSpacing : 0, 0);
            }
        }

        public override void OnFontChange(FrameworkElement control)
        {
            adjustIconPadding(control as Button);
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            // The Button FontSize value is nonsense until we get the loaded notification.  We'll update the
            // icon padding now since it is fontsize-based.
            //
            adjustIconPadding(sender as Button);
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
