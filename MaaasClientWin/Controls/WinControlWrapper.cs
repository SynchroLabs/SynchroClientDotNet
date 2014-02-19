using MaaasCore;
using Newtonsoft.Json.Linq;
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
        FrameworkElement _control;
        public WinFontSetter(FrameworkElement control)
        {
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
            }
        }

        public override void SetSize(double size)
        {
            var property = _control.GetType().GetRuntimeProperty("FontSize");
            if (property != null)
            {
                property.SetValue(_control, size);
            }
        }

        public override void SetBold(bool bold)
        {
            FontWeight fontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
            var property = _control.GetType().GetRuntimeProperty("FontWeight");
            if (property != null)
            {
                property.SetValue(_control, fontWeight);
            }
        }

        public override void SetItalic(bool italic)
        {
            FontStyle fontStyle = italic ? FontStyle.Italic : FontStyle.Normal;
            var property = _control.GetType().GetRuntimeProperty("FontStyle");
            if (property != null)
            {
                property.SetValue(_control, fontStyle);
            }
        }
    }

    class WinControlWrapper : ControlWrapper
    {
        protected FrameworkElement _control;
        public FrameworkElement Control { get { return _control; } }

        protected WinPageView _pageView;
        public WinPageView PageView { get { return _pageView; } }

        public WinControlWrapper(WinPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control) :
            base(stateManager, viewModel, bindingContext)
        {
            _pageView = pageView;
            _control = control;
        }

        public WinControlWrapper(ControlWrapper parent, BindingContext bindingContext, FrameworkElement control = null) :
            base(parent, bindingContext)
        {
            _pageView = ((WinControlWrapper)parent).PageView;
            _control = control;
        }

        public Orientation ToOrientation(object value, Orientation defaultOrientation = Orientation.Horizontal)
        {
            if (value is Orientation)
            {
                return (Orientation)value;
            }

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

        public HorizontalAlignment ToHorizontalAlignment(object value, HorizontalAlignment defaultAlignment = HorizontalAlignment.Left)
        {
            if (value is HorizontalAlignment)
            {
                return (HorizontalAlignment)value;
            }

            HorizontalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Left")
            {
                alignment = HorizontalAlignment.Left;
            }
            if (alignmentValue == "Right")
            {
                alignment = HorizontalAlignment.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = HorizontalAlignment.Center;
            }
            return alignment;
        }

        public VerticalAlignment ToVerticalAlignment(object value, VerticalAlignment defaultAlignment = VerticalAlignment.Top)
        {
            if (value is VerticalAlignment)
            {
                return (VerticalAlignment)value;
            }

            VerticalAlignment alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Top")
            {
                alignment = VerticalAlignment.Top;
            }
            if (alignmentValue == "Bottom")
            {
                alignment = VerticalAlignment.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = VerticalAlignment.Center;
            }
            return alignment;
        }

        public static SolidColorBrush ToBrush(object value)
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

        public void processThicknessProperty(JToken thicknessAttributeValue, SetViewValue setThickness)
        {
            if (thicknessAttributeValue is JValue)
            {
                processElementProperty((string)thicknessAttributeValue, value =>
                {
                    Thickness thickness = new Thickness(ToDouble(value));
                    setThickness(thickness);
                }, "0");
            }
            else if (thicknessAttributeValue is JObject)
            {
                JObject marginObject = thicknessAttributeValue as JObject;
                Thickness thickness = new Thickness();

                processElementProperty((string)marginObject.Property("left"), value =>
                {
                    thickness.Left = ToDouble(value);
                    setThickness(thickness);
                }, "0");
                processElementProperty((string)marginObject.Property("top"), value =>
                {
                    thickness.Top = ToDouble(value);
                    setThickness(thickness);
                }, "0");
                processElementProperty((string)marginObject.Property("right"), value =>
                {
                    thickness.Right = ToDouble(value);
                    setThickness(thickness);
                }, "0");
                processElementProperty((string)marginObject.Property("bottom"), value =>
                {
                    thickness.Bottom = ToDouble(value);
                    setThickness(thickness);
                }, "0");
            }
        }

        static Thickness defaultThickness = new Thickness(0, 0, 10, 10);

        protected void applyFrameworkElementDefaults(FrameworkElement element)
        {
            //element.Margin = defaultThickness;
            element.HorizontalAlignment = HorizontalAlignment.Left;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            Util.debug("Processing framework element properties");
            processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));
            processElementProperty((string)controlSpec["height"], value => this.Control.Height = ToDeviceUnits(value));
            processElementProperty((string)controlSpec["width"], value => this.Control.Width = ToDeviceUnits(value));
            processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = ToDouble(value));
            processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = ToDouble(value));
            processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDouble(value));
            processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDouble(value));
            processElementProperty((string)controlSpec["opacity"], value => this.Control.Opacity = ToDouble(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Visibility = ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed);
            processThicknessProperty(controlSpec["margin"], value => this.Control.Margin = (Thickness)value);
            processFontAttribute(controlSpec, new WinFontSetter(this.Control));

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            processElementPropertyIfPresent((string)controlSpec["enabled"], "IsEnabled", value => ToBoolean(value));
            processElementPropertyIfPresent((string)controlSpec["background"], "Background", value => ToBrush(value));
            processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => ToBrush(value));
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
                case "command":
                    controlWrapper = new WinCommandWrapper(parent, bindingContext, controlSpec);
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
                case "password":
                    controlWrapper = new WinPasswordBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "picker":
                    controlWrapper = new WinPickerWrapper(parent, bindingContext, controlSpec);
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
                    Util.debug("WARNING: Unable to create control of type: " + controlSpec["control"]);
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
