using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MaaasClientWin.Controls
{
    public class WinFontSetter : FontSetter
    {
        WinControlWrapper _controlWrapper;
        FrameworkElement _control;
        public WinFontSetter(WinControlWrapper controlWrapper, FrameworkElement control)
        {
            _controlWrapper = controlWrapper;
            _control = control;
        }

        public override void SetFaceType(FontFaceType faceType)
        {
            FontFamily fontFamily = null;
            switch (faceType)
            {
                case FontFaceType.FONT_DEFAULT:
                case FontFaceType.FONT_SANSERIF:
                    fontFamily = new FontFamily("Segoe UI");
                    break;
                case FontFaceType.FONT_SERIF:
                    fontFamily = new FontFamily("Cambria");
                    break;
                case FontFaceType.FONT_MONOSPACE:
                    fontFamily = new FontFamily("Courier New");
                    break;
            }

            var property = _control.GetType().GetRuntimeProperty("FontFamily");
            if (property != null)
            {
                property.SetValue(_control, fontFamily);
                _controlWrapper.OnFontChange(_control);
            }
        }

        public override void SetSize(double size)
        {
            var property = _control.GetType().GetRuntimeProperty("FontSize");
            if (property != null)
            {
                property.SetValue(_control, size);
                _controlWrapper.OnFontChange(_control);
            }
        }

        public override void SetBold(bool bold)
        {
            FontWeight fontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            var property = _control.GetType().GetRuntimeProperty("FontWeight");
            if (property != null)
            {
                property.SetValue(_control, fontWeight);
                _controlWrapper.OnFontChange(_control);
            }
        }

        public override void SetItalic(bool italic)
        {
            FontStyle fontStyle = italic ? FontStyle.Italic : FontStyle.Normal;
            var property = _control.GetType().GetRuntimeProperty("FontStyle");
            if (property != null)
            {
                property.SetValue(_control, fontStyle);
                _controlWrapper.OnFontChange(_control);
            }
        }
    }

    public class WinControlWrapper : ControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinControlWrapper");

        protected FrameworkElement _control;
        public FrameworkElement Control { get { return _control; } }

        protected WinPageView _pageView;
        public WinPageView PageView { get { return _pageView; } }

        protected Boolean _heightSpecified = false;
        protected Boolean _widthSpecified = false;

        protected Brush _defaultForeground = null;
        protected Brush _defaultBackground = null;

        public WinControlWrapper(WinPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control) :
            base(stateManager, viewModel, bindingContext)
        {
            _pageView = pageView;
            _control = control;
        }

        public WinControlWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            _pageView = ((WinControlWrapper)parent).PageView;
        }

        public virtual void OnFontChange(FrameworkElement control)
        {
        }

        public Orientation ToOrientation(JToken value, Orientation defaultOrientation = Orientation.Horizontal)
        {
            Orientation orientation = defaultOrientation;
            string orientationValue = ToString(value);
            if (orientationValue == "Horizontal")
            {
                orientation = Orientation.Horizontal;
            }
            else if (orientationValue == "Vertical")
            {
                orientation = Orientation.Vertical;
            }
            return orientation;
        }

        public HorizontalAlignment ToHorizontalAlignment(JToken value, HorizontalAlignment defaultAlignment = HorizontalAlignment.Left)
        {
            HorizontalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Left")
            {
                alignment = HorizontalAlignment.Left;
            }
            else if (alignmentValue == "Right")
            {
                alignment = HorizontalAlignment.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = HorizontalAlignment.Center;
            }
            else if (alignmentValue == "Stretch")
            {
                alignment = HorizontalAlignment.Stretch;
            }
            return alignment;
        }

        public VerticalAlignment ToVerticalAlignment(JToken value, VerticalAlignment defaultAlignment = VerticalAlignment.Top)
        {
            VerticalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Top")
            {
                alignment = VerticalAlignment.Top;
            }
            else if (alignmentValue == "Bottom")
            {
                alignment = VerticalAlignment.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = VerticalAlignment.Center;
            }
            else if (alignmentValue == "Stretch")
            {
                alignment = VerticalAlignment.Stretch;
            }
            return alignment;
        }

        public SolidColorBrush ToBrush(JToken value)
        {
            ColorARGB color = ControlWrapper.getColor(ToString(value));
            if (color != null)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(color.a, color.r, color.g, color.b));
            }
            else
            {
                return null;
            }
        }

        public FontWeight ToFontWeight(JToken value)
        {
            String weight = ToString(value);

            var property = typeof(FontWeights).GetRuntimeProperty(weight);
            if (property != null)
            {
                return (FontWeight)property.GetValue(null);
            }
            return FontWeights.Normal;
        }

        public delegate void SetViewThickness(Thickness thickness);
        public delegate Thickness GetViewThickness();

        public void processThicknessProperty(JObject controlSpec, String attributeName, GetViewThickness getThickness, SetViewThickness setThickness)
        {
            processElementProperty(controlSpec, attributeName + ".left", attributeName, value =>
            {
                Thickness thickness = (Thickness)getThickness();
                thickness.Left = this.ToDeviceUnits(ToDouble(value));
                setThickness(thickness);
            });
            processElementProperty(controlSpec, attributeName + ".top", attributeName, value =>
            {
                Thickness thickness = (Thickness)getThickness();
                thickness.Top = this.ToDeviceUnits(ToDouble(value));
                setThickness(thickness);
            });
            processElementProperty(controlSpec, attributeName + ".right", attributeName, value =>
            {
                Thickness thickness = (Thickness)getThickness();
                thickness.Right = this.ToDeviceUnits(ToDouble(value));
                setThickness(thickness);
            });
            processElementProperty(controlSpec, attributeName + ".bottom", attributeName, value =>
            {
                Thickness thickness = (Thickness)getThickness();
                thickness.Bottom = this.ToDeviceUnits(ToDouble(value));
                setThickness(thickness);
            });
        }

        // static Thickness defaultThickness = new Thickness(0, 0, 10, 10);

        protected void applyFrameworkElementDefaults(FrameworkElement element, bool applyMargins = true)
        {
            if (applyMargins)
            {
                element.Margin = new Thickness(0, 0, ToDeviceUnits(10), ToDeviceUnits(10));
            }
            element.HorizontalAlignment = HorizontalAlignment.Left;
            element.VerticalAlignment = VerticalAlignment.Top;
        }

        protected void setHeight(FrameworkElement control, JToken value)
        {
            string heightString = ToString(value);
            if (heightString.IndexOf("*") >= 0)
            {
                logger.Debug("Got star height string: {0}", value);
                control.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                control.Height = ToDeviceUnits(value);
            }
            _heightSpecified = true;
        }

        protected void setWidth(FrameworkElement control, JToken value)
        {
            string widthString = ToString(value);
            if (widthString.IndexOf("*") >= 0)
            {
                logger.Debug("Got star width string: {0}", value);
                control.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                control.Width = ToDeviceUnits(value);
            }
            _widthSpecified = true;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            logger.Debug("Processing framework element properties");
            processElementProperty(controlSpec, "name", value => this.Control.Name = ToString(value));
            processElementProperty(controlSpec, "horizontalAlignment", value => this.Control.HorizontalAlignment = ToHorizontalAlignment(value));
            processElementProperty(controlSpec, "verticalAlignment", value => this.Control.VerticalAlignment = ToVerticalAlignment(value));
            processElementProperty(controlSpec, "height", value => setHeight(this.Control, value));
            processElementProperty(controlSpec, "width", value => setWidth(this.Control, value));
            processElementProperty(controlSpec, "minheight", value => this.Control.MinHeight = ToDeviceUnits(value));
            processElementProperty(controlSpec, "minwidth", value => this.Control.MinWidth = ToDeviceUnits(value));
            processElementProperty(controlSpec, "maxheight", value => this.Control.MaxHeight = ToDeviceUnits(value));
            processElementProperty(controlSpec, "maxwidth", value => this.Control.MaxWidth = ToDeviceUnits(value));
            processElementProperty(controlSpec, "opacity", value => this.Control.Opacity = ToDouble(value));
            processElementProperty(controlSpec, "visibility", value =>
            {
                Visibility visibility = ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;
                if (this.Control.Visibility != visibility)
                {
                    // This is kind of a mess.  When the child of a stackpanel (for which we use SynchroGrid) goes to collapsed state, the
                    // parent grid is invalidated and a layout pass happens to account for the newly vanished child element.  Unfortunately,
                    // at least in some cases, the reverse is not true.  When an element that was hidden by the grid (by setting its cell
                    // to zero w/h) later it becomes visible, there is no notification of the parent and no new layout pass.  So we force that
                    // here in this special case (if the visibility is currently not visible, it changes to visible, and the containing parent
                    // is a SynchroGrid, then we force a layout pass).
                    //
                    this.Control.Visibility = visibility;
                    if (this.Control.Visibility == Visibility.Visible)
                    {
                        SynchroGrid grid = this.Control.Parent as SynchroGrid;
                        if (grid != null)
                        {
                            grid.InvalidateArrange();
                        }
                    }
                }
            });
            processThicknessProperty(controlSpec, "margin", () => this.Control.Margin, value => this.Control.Margin = (Thickness)value);
            processFontAttribute(controlSpec, new WinFontSetter(this, this.Control));

            // There are groups of controls which share some properties (IsEnabled, Foreground, and Background), but those properties are
            // all implemented in various derived classes.  We'll handle them all here semi-centrally.
            //
            // Note: For Foreground and Background, our intent is that setting these values to null should revert them to their
            //       default values.  If those values had been set via XAML, we could use ClearValue.  Since they weren't, we have
            //       to record the default value when we set it to another value for the first time, then restore it if we ever set
            //       the value back to null.
            //
            if (this.Control is Control)
            {
                var control = this.Control as Control;
                processElementProperty(controlSpec, "enabled", value => control.IsEnabled = ToBoolean(value));
                processElementProperty(controlSpec, "foreground", value =>
                {
                    _defaultForeground = _defaultForeground ?? control.Foreground ?? new SolidColorBrush(); // Transparent brush
                    control.Foreground = ToBrush(value) ?? _defaultForeground;
                });
                processElementProperty(controlSpec, "background", value =>
                {
                    _defaultBackground = _defaultBackground ?? control.Background ?? new SolidColorBrush(); // Transparent brush
                    control.Background = ToBrush(value) ?? _defaultBackground;
                });
            }
            else if (this.Control is Panel)
            {
                var control = this.Control as Panel;
                processElementProperty(controlSpec, "background", value =>
                {
                    _defaultBackground = _defaultBackground ?? control.Background ?? new SolidColorBrush(); // Transparent brush
                    control.Background = ToBrush(value) ?? _defaultBackground;
                });
            }
            else if (this.Control is Border)
            {
                var control = this.Control as Border;
                processElementProperty(controlSpec, "background", value =>
                {
                    _defaultBackground = _defaultBackground ?? control.Background ?? new SolidColorBrush(); // Transparent brush
                    control.Background = ToBrush(value) ?? _defaultBackground;
                });
            }
            else if (this.Control is TextBlock)
            {
                var control = this.Control as TextBlock;
                processElementProperty(controlSpec, "foreground", value =>
                {
                    _defaultForeground = _defaultForeground ?? control.Foreground ?? new SolidColorBrush(); // Transparent brush
                    control.Foreground = ToBrush(value) ?? _defaultForeground;
                });
            }
        }

        public WinControlWrapper getChildControlWrapper(FrameworkElement control)
        {
            // Find the child control wrapper whose control matches the supplied value...
            foreach (WinControlWrapper child in this.ChildControls)
            {
                if (child.Control == control)
                {
                    return child;
                }
            }

            return null;
        }

        public static WinControlWrapper WrapControl(WinPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control)
        {
            return new WinControlWrapper(pageView, stateManager, viewModel, bindingContext, control);
        }

        public static WinControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            WinControlWrapper controlWrapper = null;

            switch ((string)controlSpec["control"])
            {
                case "border":
                    controlWrapper = new WinBorderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new WinButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new WinCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "commandBar.button":
                    controlWrapper = new WinCommandWrapper(parent, bindingContext, controlSpec);
                    break;
                case "commandBar.toggle":
                    controlWrapper = new WinToggleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new WinTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "gridview":
                    controlWrapper = new WinGridViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "image":
                    controlWrapper = new WinImageWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listbox":
                    controlWrapper = new WinListBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listview":
                    controlWrapper = new WinListViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "location":
                    controlWrapper = new WinLocationWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new WinPasswordBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "picker":
                    controlWrapper = new WinPickerWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressbar":
                    controlWrapper = new WinSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressring":
                    controlWrapper = new WinProgressRingWrapper(parent, bindingContext, controlSpec);
                    break;
                case "rectangle":
                    controlWrapper = new WinRectangleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "scrollview":
                    controlWrapper = new WinScrollWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new WinSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new WinStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
                case "text":
                    controlWrapper = new WinTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toggle":
                    controlWrapper = new WinToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
                case "webview":
                    controlWrapper = new WinWebViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "wrappanel":
                    controlWrapper = new WinWrapPanelWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, WinControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) => 
            {
                WinControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
                if (controlWrapper == null)
                {
                    logger.Warn("WARNING: Unable to create control of type: {0}", controlSpec["control"]);
                }
                else if (OnCreateControl != null)
                {
                    if (controlWrapper.IsVisualElement)
                    {
                        OnCreateControl(controlSpec, controlWrapper);
                    }
                }
            });
        }
    }
}
