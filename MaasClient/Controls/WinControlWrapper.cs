﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MaasClient.Controls
{
    class WinControlWrapper : ControlWrapper
    {
        protected FrameworkElement _control;
        public FrameworkElement Control { get { return _control; } }

        public WinControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control) :
            base(stateManager, viewModel, bindingContext)
        {
            _control = control;
        }

        public WinControlWrapper(ControlWrapper parent, BindingContext bindingContext, FrameworkElement control = null) :
            base(parent, bindingContext)
        {
            _control = control;
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
                    double marginThickness = Converter.ToDouble(value);
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
                    thickness.Left = Converter.ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("top"), value =>
                {
                    thickness.Top = Converter.ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("right"), value =>
                {
                    thickness.Right = Converter.ToDouble(value);
                    this.Control.Margin = thickness;
                }, "0");
                processElementProperty((string)marginObject.Property("bottom"), value =>
                {
                    thickness.Bottom = Converter.ToDouble(value);
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
            processElementProperty((string)controlSpec["name"], value => this.Control.Name = Converter.ToString(value));
            processElementProperty((string)controlSpec["height"], value => this.Control.Height = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["width"], value => this.Control.Width = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["opacity"], value => this.Control.Opacity = Converter.ToDouble(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Visibility = Converter.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed);
            processMarginProperty(controlSpec["margin"]);

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            processElementPropertyIfPresent((string)controlSpec["fontsize"], "FontSize", value => Converter.ToDouble(value));
            processElementPropertyIfPresent((string)controlSpec["fontweight"], "FontWeight", value => Converter.ToFontWeight(value));
            processElementPropertyIfPresent((string)controlSpec["enabled"], "IsEnabled", value => Converter.ToBoolean(value));
            processElementPropertyIfPresent((string)controlSpec["background"], "Background", value => Converter.ToBrush(value));
            processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => Converter.ToBrush(value));
        }

        public static WinControlWrapper getControlWrapper(FrameworkElement control)
        {
            return (WinControlWrapper)control.Tag;
        }

        public static WinControlWrapper WrapControl(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, FrameworkElement control)
        {
            return new WinControlWrapper(stateManager, viewModel, bindingContext, control);
        }

        public static WinControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            WinControlWrapper controlWrapper = null;

            switch ((string)controlSpec["type"])
            {
                case "text":
                    controlWrapper = new WinTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new WinTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new WinPasswordBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new WinButtonWrapper(parent, bindingContext, controlSpec);
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
                case "toggle":
                    controlWrapper = new WinToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new WinSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new WinCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new WinStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
                controlWrapper.Control.Tag = controlWrapper;
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, WinControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) => 
            {
                WinControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
                if (OnCreateControl != null)
                {
                    OnCreateControl(controlSpec, controlWrapper);
                }
            });
        }
    }
}
