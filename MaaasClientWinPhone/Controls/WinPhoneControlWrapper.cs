using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Graphics.Display;


namespace MaaasClientWinPhone.Controls
{
    class WinPhoneControlWrapper : ControlWrapper
    {
        protected FrameworkElement _control;
        public FrameworkElement Control { get { return _control; } }

        public WinPhoneControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control) :
            base(stateManager, viewModel, bindingContext)
        {
            _control = control;
        }

        public WinPhoneControlWrapper(ControlWrapper parent, BindingContext bindingContext, FrameworkElement control = null) :
            base(parent, bindingContext)
        {
            _control = control;
        }

        protected static double WindowsPxFromMaaasUnits(double maaasUnits)
        {
            // A MaaasUnit is 1/160 of an inch.  A Windows px (device-independant pixel) is 1/96 of an inch.
            // 
            return maaasUnits * 96f / 160f;
        }

        public static double WindowsPxFromTypographicPoints(double typographicPoints)
        {
            Util.debug("Display properties - logical DPI: " + DisplayProperties.LogicalDpi);
            Util.debug("Display properties - resolution scale: " + DisplayProperties.ResolutionScale);

            // A typographic point is 1/72 of an inch.  A Windows px (device-independant pixel) is 1/96 of an inch.
            //
            return typographicPoints * 96f / 72f;
        }

        public static SolidColorBrush ToBrush(object value)
        {
            ColorARGB color = ControlWrapper.getColor(ToString(value));
            if (color != null)
            {
                return new SolidColorBrush(Color.FromArgb(color.a, color.r, color.g, color.b));
            }
            else
            {
                return null;
            }
        }

        public static FontWeight ToFontWeight(object value)
        {
            String weight = ToString(value);

            var property = typeof(FontWeights).GetRuntimeProperty(weight);
            if (property != null)
            {
                return (FontWeight)property.GetValue(null);
            }
            return FontWeights.Normal;
        }

        public delegate object ConvertValue(object value);

        // This method allows us to do some runtime reflection to see if a property exists on an element, and if so, to bind to it.  This
        // is needed because there are common properties of FrameworkElement instances that are repeated in different class trees.  For
        // example, "IsEnabled" exists as a property on most instances of FrameworkElement objects, though it is not defined in a single
        // common base class.
        //
        protected void processElementPropertyIfPresent(string attributeValue, string propertyName, ConvertValue convertValue = null)
        {
            if (attributeValue != null)
            {
                if (convertValue == null)
                {
                    convertValue = value => value;
                }
                var property = this.Control.GetType().GetRuntimeProperty(propertyName);
                if (property != null)
                {
                    processElementProperty(attributeValue, value => property.SetValue(this.Control, convertValue(value), null));
                }
            }
        }

        protected void processMarginProperty(JToken margin)
        {
            Thickness thickness = new Thickness();

            if (margin is JValue)
            {
                processElementProperty((string)margin, value =>
                {
                    double marginThickness = ToDouble(value);
                    thickness.Left = marginThickness;
                    thickness.Top = marginThickness;
                    thickness.Right = marginThickness;
                    thickness.Bottom = marginThickness;
                    this.Control.Margin = thickness;
                }, "0");
            }
            else if (margin is JObject)
            {
                JObject marginObject = margin as JObject;
                processElementProperty((string)marginObject.Property("left"), value =>
                {
                    thickness.Left = ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("top"), value =>
                {
                    thickness.Top = ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("right"), value =>
                {
                    thickness.Right = ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("bottom"), value =>
                {
                    thickness.Bottom = ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
            }
        }

        static Thickness defaultThickness = new Thickness(0, 0, 10, 10);

        protected void applyFrameworkElementDefaults(FrameworkElement element)
        {
            element.Margin = defaultThickness;
            element.HorizontalAlignment = HorizontalAlignment.Left;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            // !!! TODO: more common framework element properties...
            //
            //           VerticalAlignment [ Top, Center, Bottom, Stretch ]
            //           HorizontalAlignment [ Left, Center, Right, Stretch ] 
            //           Maring, padding, border, etc.
            //

            Util.debug("Processing framework element properties");
            processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));
            processElementProperty((string)controlSpec["height"], value => this.Control.Height = ToDouble(value));
            processElementProperty((string)controlSpec["width"], value => this.Control.Width = ToDouble(value));
            processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = ToDouble(value));
            processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = ToDouble(value));
            processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDouble(value));
            processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDouble(value));
            processElementProperty((string)controlSpec["opacity"], value => this.Control.Opacity = ToDouble(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Visibility = ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed);
            processMarginProperty(controlSpec["margin"]);

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            processElementPropertyIfPresent((string)controlSpec["fontsize"], "FontSize", value => WindowsPxFromTypographicPoints(ToDouble(value)));
            processElementPropertyIfPresent((string)controlSpec["fontweight"], "FontWeight", value => ToFontWeight(value));
            processElementPropertyIfPresent((string)controlSpec["enabled"], "IsEnabled", value => ToBoolean(value));
            processElementPropertyIfPresent((string)controlSpec["background"], "Background", value => ToBrush(value));
            processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => ToBrush(value));
        }

        public WinPhoneControlWrapper getChildControlWrapper(FrameworkElement control)
        {
            // Find the child control wrapper whose control matches the supplied value...
            foreach (WinPhoneControlWrapper child in this.ChildControls)
            {
                if (child.Control == control)
                {
                    return child;
                }
            }

            return null;
        }

        public static WinPhoneControlWrapper WrapControl(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control)
        {
            return new WinPhoneControlWrapper(stateManager, viewModel, bindingContext, control);
        }

        public static WinPhoneControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            WinPhoneControlWrapper controlWrapper = null;

            switch ((string)controlSpec["type"])
            {
                case "text":
                    controlWrapper = new WinPhoneTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new WinPhoneTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new WinPhonePasswordBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new WinPhoneButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "image":
                    controlWrapper = new WinPhoneImageWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listbox":
                    controlWrapper = new WinPhoneListBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listview":
                    // !!! controlWrapper = new WinPhoneListViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toggle":
                    controlWrapper = new WinPhoneToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new WinPhoneSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new WinPhoneCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new WinPhoneStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
                case "rectangle":
                    controlWrapper = new WinPhoneRectangleWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, WinPhoneControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) => 
            {
                WinPhoneControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
                if (controlWrapper == null)
                {
                    Util.debug("WARNING: Unable to create control of type: " + controlSpec["type"]);
                }
                else if (OnCreateControl != null)
                {
                    OnCreateControl(controlSpec, controlWrapper);
                }
            });
        }
    }
}
