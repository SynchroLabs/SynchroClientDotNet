using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Newtonsoft.Json.Linq;
using Android.Util;
using Android.Graphics;

namespace MaaasClientAndroid.Controls
{
    class WrapperHolder : Java.Lang.Object 
    {
        public readonly AndroidControlWrapper Value;

        public WrapperHolder(AndroidControlWrapper value)
        {
                this.Value = value;
        }
    }

    class AndroidControlWrapper : ControlWrapper
    {
        protected View _control;
        public View Control { get { return _control; } }

        protected int _height = ViewGroup.LayoutParams.WrapContent;
        protected int _width = ViewGroup.LayoutParams.WrapContent;

        public AndroidControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control) :
            base(stateManager, viewModel, bindingContext)
        {
            _control = control;
        }

        public AndroidControlWrapper(ControlWrapper parent, BindingContext bindingContext, View control = null) :
            base(parent, bindingContext)
        {
            _control = control;
        }

        public void updateSize()
        {
            if (_control.LayoutParameters == null)
            {
                _control.LayoutParameters = new ViewGroup.LayoutParams(_width, _height);
            }
            else
            {
                _control.LayoutParameters.Width = _width;
                _control.LayoutParameters.Height = _height;
            }
            _control.RequestLayout();
        }

        public int Width 
        {
            get { return _width; }
            set 
            {
                _width = value;
                _control.SetMinimumWidth(value);
                this.updateSize();
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                _control.SetMinimumHeight(value);
                this.updateSize();
            }
        }

        public static Color ToColor(object value)
        {
            ColorARGB color = ControlWrapper.getColor(ToString(value));
            if (color != null)
            {
                return Color.Argb(color.a, color.r, color.g, color.b);
            }
            else
            {
                return Color.Transparent;
            }
        }

        protected void applyFrameworkElementDefaults(View element)
        {
            // !!! This could be a little more thourough ;)
        }

        public double ToAndroidDpFromTypographicPoints(object value)
        {
            // A typographic point is 1/72 of an inch.  Convert to logical pixel value for device.
            //
            double typographicPoints = ToDouble(value);
            return typographicPoints * 160f / 72f;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            // !!! This could be a little more thourough ;)
            Util.debug("Processing framework element properties");

            //processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));

            processElementProperty((string)controlSpec["height"], value => this.Height = (int)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["width"], value => this.Width = (int)ToDeviceUnits(value));
            this.updateSize();

            processElementProperty((string)controlSpec["minheight"], value => this.Control.SetMinimumHeight((int)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["minwidth"], value => this.Control.SetMinimumWidth((int)ToDeviceUnits(value)));

            //processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDouble(value));
            //processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDouble(value));
            //processElementProperty((string)controlSpec["opacity"], value => this.Control.Opacity = ToDouble(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Visibility = ToBoolean(value) ? ViewStates.Visible : ViewStates.Gone);
            processElementProperty((string)controlSpec["enabled"], value => this.Control.Enabled = ToBoolean(value));
            //processMarginProperty(controlSpec["margin"]);

            TextView textView = this.Control as TextView;
            if (textView != null)
            {
                // !!! These seem to be equivalent, but product fonts that are larger than on other platforms (for glyph span is the specified height, 
                //     with the total box being a fair amount larger, as opposed to most platforms where the box is the specified height).
                //
                processElementProperty((string)controlSpec["fontsize"], value => textView.SetTextSize(ComplexUnitType.Px, (float)ToDeviceUnitsFromTypographicPoints(value)));

                //processElementPropertyIfPresent((string)controlSpec["fontweight"], "FontWeight", value => ToFontWeight(value));
            }

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            //processElementPropertyIfPresent((string)controlSpec["background"], "Background", value => ToBrush(value));
            //processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => ToBrush(value));
        }

        public AndroidControlWrapper getChildControlWrapper(View control)
        {
            // Find the child control wrapper whose control matches the supplied value...
            foreach (AndroidControlWrapper child in this.ChildControls)
            {
                if (child.Control == control)
                {
                    return child;
                }
            }

            return null;
        }

        public static AndroidControlWrapper WrapControl(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control)
        {
            return new AndroidControlWrapper(stateManager, viewModel, bindingContext, control);
        }

        public static AndroidControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            AndroidControlWrapper controlWrapper = null;

            switch ((string)controlSpec["type"])
            {
                case "text":
                    controlWrapper = new AndroidTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                case "password":
                    controlWrapper = new AndroidTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new AndroidButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "image":
                    controlWrapper = new AndroidImageWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new AndroidStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toggle":
                    controlWrapper = new AndroidToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new AndroidCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new AndroidSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "rectangle":
                    controlWrapper = new AndroidRectangleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "scrollview":
                    controlWrapper = new AndroidScrollWrapper(parent, bindingContext, controlSpec);
                    break;
                case "border":
                    controlWrapper = new AndroidBorderWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
                controlWrapper.Control.Tag = new WrapperHolder(controlWrapper);
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, AndroidControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) =>
            {
                AndroidControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
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