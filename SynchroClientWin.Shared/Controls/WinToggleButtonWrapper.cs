using SynchroClientWin;
using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MaaasClientWin.Controls
{
    class WinToggleButtonWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinToggleButtonWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle.Attribute };

        // The Glyph in the Material font sits at the top of the box, such that it needs to be
        // padded down to align with adjacent text.  This factor brings the glyph baseline down to
        // the text baseline (at least with standard fonts).
        //
        static double IconTopPadFactor = 0.2;

        static double IconTextSpacing = 5;

        protected bool _isChecked = false;
        bool isChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    updateVisualState();
                }
            }
        }

        TextBlock _textControl;
        TextBlock _iconControl;

        String _caption;
        String _checkedCaption;
        String _uncheckedCaption;

        String _icon;
        String _checkedIcon;
        String _uncheckedIcon;

        Brush _color;
        Brush _checkedColor;
        Brush _uncheckedColor;

        protected void setCaption(String caption)
        {
            var button = _control as Button;
            _textControl.Text = caption;
            adjustIconPadding(button);
        }

        protected void setIcon(String icon)
        {
            var button = _control as Button;
            var container = button.Content as StackPanel;

            if (_iconControl == null)
            {
                _iconControl = new TextBlock();
                _iconControl.FontFamily = GlyphMapper.getFontFamily(); // Will trigger adjustIconPadding() on setting font family
                container.Children.Insert(0, _iconControl);
            }
            _iconControl.Text = GlyphMapper.getGlyph(icon);
        }

        protected void setColor(Brush color)
        {
            var button = _control as Button;
            button.Foreground = color;
        }

        protected void updateVisualState()
        {
            // If the user specified configuration that visually communicates the state change, then we will use only those
            // configuration elements to show the state change.  If they do not include any such elements, then we will gray
            // the toggle button out to show the unchecked state.
            //
            var isVisualStateExplicit = ((_checkedCaption != null) || (_checkedIcon != null) || (_checkedColor != null));

            if (isChecked)
            {
                if (isVisualStateExplicit)
                {
                    // One or more of the explicit checked items will be set below...
                    //
                    if (_checkedCaption != null)
                    {
                        setCaption(_checkedCaption);
                    }
                    if (_checkedIcon != null)
                    {
                        setIcon(_checkedIcon);
                    }
                    if (_checkedColor != null)
                    {
                        setColor(_checkedColor);
                    }
                }
                else
                {
                    // There was no explicit visual state specified, so we will use default color for checked
                    //
                    setColor(_color);
                }
            }
            else
            {
                if (isVisualStateExplicit)
                {
                    // One or more of the explicit unchecked items will be set below...
                    //
                    if (_uncheckedCaption != null)
                    {
                        setCaption(_uncheckedCaption);
                    }
                    if (_uncheckedIcon != null)
                    {
                        setIcon(_uncheckedIcon);
                    }
                    if (_uncheckedColor != null)
                    {
                        setColor(_uncheckedColor);
                    }
                }
                else
                {
                    // There was no explicit visual state specified, so we will use "gray" for unchecked
                    //
                    setColor(new SolidColorBrush(Colors.Gray));
                }
            }
        }

        public WinToggleButtonWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating toggle button element");
            Button button = new Button();
            this._control = button;

            // We're going to set a default size, since the platform default is really small.
            // 
            button.FontSize = ToDeviceUnitsFromTypographicPoints(new JValue(10.0f));

            _color = button.Foreground;

            applyFrameworkElementDefaults(button);

            var container = new StackPanel();
            container.Orientation = Orientation.Horizontal;
            container.VerticalAlignment = VerticalAlignment.Center;
            button.Content = container;

            // The text control needs to be added whether or not there is any text.  Since the Material Design icon
            // font doesn't have anything below the baseline, we need the text control so that auto-height controls
            // will be the correct size on WinPhone when they have an icon only.
            //
            _textControl = new TextBlock();
            container.Children.Add(_textControl);

            button.Loaded += Button_Loaded;

            if (ToBoolean(controlSpec["borderless"], true))
            {
                button.BorderThickness = new Thickness(0);
                button.Background = null;
            }

            processElementProperty(controlSpec, "caption", value =>
            {
                _caption = ToString(value);
                setCaption(_caption);
                updateVisualState();
            });

            processElementProperty(controlSpec, "checkedcaption", value =>
            {
                _checkedCaption = ToString(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "uncheckedcaption", value =>
            {
                _uncheckedCaption = ToString(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "icon", value =>
            {
                _icon = ToString(value);
                setIcon(_icon);
                updateVisualState();
            });

            processElementProperty(controlSpec, "checkedicon", value =>
            {
                _checkedIcon = ToString(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "uncheckedicon", value =>
            {
                _uncheckedIcon = ToString(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "color", "foreground", value =>
            {
                // Color is set in base class, we're just going to record the value here for later
                _color = ToBrush(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "checkedcolor", value =>
            {
                _checkedColor = ToBrush(value);
                updateVisualState();
            });

            processElementProperty(controlSpec, "uncheckedcolor", value =>
            {
                _uncheckedColor = ToBrush(value);
                updateVisualState();
            });

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new JValue(isChecked); }, value => isChecked = ToBoolean(value)))
            {
                processElementProperty(controlSpec, "value", value => isChecked = ToBoolean(value));
            }

            button.Click += button_Click;
        }

        protected void adjustIconPadding(Button button)
        {
            // The Glyph in the Material font needs to be padded down to align with adjacent text.  
            // We also add padding between the icon and text if there is text currently set.
            //
            if (_iconControl != null)
            {
                var iconTopPad = button.FontSize * IconTopPadFactor;
                _iconControl.Padding = new Thickness(0, iconTopPad, (_textControl.Text.Length > 0) ? IconTextSpacing : 0, 0);
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
            isChecked = !isChecked;

            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                logger.Debug("Toggle toggled with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}

