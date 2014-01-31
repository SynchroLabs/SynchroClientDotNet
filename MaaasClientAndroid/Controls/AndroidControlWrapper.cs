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

    public class AndroidFontSetter : FontSetter
    {
        TextView _control = null;
        bool _bold = false;
        bool _italic = false;

        public AndroidFontSetter(View control)
        {
            _control = control as TextView;
        }

        // !!! The SetTypeface method used below takes an "extra" stlye param, which is documented as:
        //
        //         "Sets the typeface and style in which the text should be displayed, and turns on the fake bold and italic bits
        //          in the Paint if the Typeface that you provided does not have all the bits in the style that you specified."
        //
        //     When using this method with such a font (like the default system monospace font), this works fine unless you are
        //     trying to set the style from any non-normal value to normal, in which case it fails to restore it to normal.
        //     Not sure if this is an Android TextView bug or a Xamarin bug, but would guess the former.  Setting the typeface
        //     to a face that does support the style bits, then setting it to the proper typeface with the extra style param
        //     seems to work (and doesn't produce any visible flickering or other artifacts).  So we're going with that for now.
        //
        protected void setStyledTypeface(Typeface tf)
        {
            TypefaceStyle tfStyle = getTypefaceStyle(_bold, _italic);
            tf = Typeface.Create(tf, tfStyle);
            if (tfStyle == TypefaceStyle.Normal)
            {
                _control.Typeface = Typeface.Default; // This is the hackaround described above
            }
            _control.SetTypeface(tf, tfStyle);
        }

        public override void SetFaceType(FontFaceType faceType)
        {
            if (_control != null)
            {
                Typeface tf = null;
                
                switch (faceType)
                {
                    case FontFaceType.FONT_DEFAULT:
                        tf = Typeface.Default;
                        break;
                    case FontFaceType.FONT_SANSERIF:
                        tf = Typeface.SansSerif;
                        break;
                    case FontFaceType.FONT_SERIF:
                        tf = Typeface.Serif;
                        break;
                    case FontFaceType.FONT_MONOSPACE:
                        tf = Typeface.Monospace;
                        break;
                }

                if (tf != null)
                {
                    this.setStyledTypeface(tf);
                }
            }
        }

        public override void SetSize(double size)
        {
            if (_control != null)
            {
                // !!! These seem to be equivalent, but produce fonts that are larger than on other platforms (for glyph span is the specified height, 
                //     with the total box being a fair amount larger, as opposed to most platforms where the box is the specified height).
                //
                _control.SetTextSize(ComplexUnitType.Px, (float)size);
            }
        }

        protected bool isBold(TypefaceStyle tfStyle)
        {
            return ((tfStyle == TypefaceStyle.Bold) || (tfStyle == TypefaceStyle.BoldItalic));
        }

        protected bool isItalic(TypefaceStyle tfStyle)
        {
            return ((tfStyle == TypefaceStyle.Italic) || (tfStyle == TypefaceStyle.BoldItalic));
        }

        protected TypefaceStyle getTypefaceStyle(bool bold, bool italic)
        {
            if (bold && italic)
            {
                return TypefaceStyle.BoldItalic;
            }
            else if (bold)
            {
                return TypefaceStyle.Bold;
            }
            else if (italic)
            {
                return TypefaceStyle.Italic;
            }
            else
            {
                return TypefaceStyle.Normal;
            }
        }

        public override void SetBold(bool bold)
        {
            _bold = bold;
            if (_control != null)
            {
                Typeface tf = _control.Typeface;
                this.setStyledTypeface(tf);
            }
        }

        public override void SetItalic(bool italic)
        {
            _italic = italic;
            if (_control != null)
            {
                Typeface tf = _control.Typeface;
                this.setStyledTypeface(tf);
            }
        }
    }

    public abstract class ThicknessSetter
    {
        public virtual void SetThickness(int thickness)
        {
            this.SetThicknessTop(thickness);
            this.SetThicknessLeft(thickness);
            this.SetThicknessBottom(thickness);
            this.SetThicknessRight(thickness);
        }
        public abstract void SetThicknessLeft(int thickness);
        public abstract void SetThicknessTop(int thickness);
        public abstract void SetThicknessRight(int thickness);
        public abstract void SetThicknessBottom(int thickness);
    }

    public class MarginThicknessSetter : ThicknessSetter
    {
        protected View _control;

        public MarginThicknessSetter(AndroidControlWrapper controlWrapper)
        {
            controlWrapper.InitializeLayoutParameters();
            _control = controlWrapper.Control;
        }

        public override void SetThicknessLeft(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.LeftMargin = thickness;
            }
        }

        public override void SetThicknessTop(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.TopMargin = thickness;
            }
        }

        public override void SetThicknessRight(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.RightMargin = thickness;
            }
        }

        public override void SetThicknessBottom(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.BottomMargin = thickness;
            }
        }
    }

    public class PaddingThicknessSetter : ThicknessSetter
    {
        protected View _control;

        public PaddingThicknessSetter(View control)
        {
            _control = control;
        }

        public override void SetThickness(int thickness)
        {
            _control.SetPadding(thickness, thickness, thickness, thickness);
        }

        public override void SetThicknessLeft(int thickness)
        {
            _control.SetPadding(thickness, _control.PaddingTop, _control.PaddingRight, _control.PaddingBottom);
        }

        public override void SetThicknessTop(int thickness)
        {
            _control.SetPadding(_control.PaddingLeft, thickness, _control.PaddingRight, _control.PaddingBottom);
        }

        public override void SetThicknessRight(int thickness)
        {
            _control.SetPadding(_control.PaddingLeft, _control.PaddingTop, thickness, _control.PaddingBottom);
        }

        public override void SetThicknessBottom(int thickness)
        {
            _control.SetPadding(_control.PaddingLeft, _control.PaddingTop, _control.PaddingRight, thickness);
        }
    }

    public class AndroidControlWrapper : ControlWrapper
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

        public AndroidControlWrapper(ControlWrapper parent, BindingContext bindingContext) :
            base(parent, bindingContext)
        {
        }

        public void InitializeLayoutParameters()
        {
            if (_control.LayoutParameters == null)
            {
                _control.LayoutParameters = new ViewGroup.MarginLayoutParams(_width, _height);
            }
        }

        public void updateSize()
        {
            InitializeLayoutParameters();

            // We don't want to overwrite an actual height/width value with WrapContent...
            //
            if (_width != ViewGroup.LayoutParams.WrapContent)
            {
                _control.LayoutParameters.Width = _width;
            }

            if (_height != ViewGroup.LayoutParams.WrapContent)
            {
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

        public GravityFlags ToHorizontalAlignment(object value, GravityFlags defaultAlignment = GravityFlags.Left)
        {
            if (value is GravityFlags)
            {
                return (GravityFlags)value;
            }

            GravityFlags alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Left")
            {
                alignment = GravityFlags.Left;
            }
            if (alignmentValue == "Right")
            {
                alignment = GravityFlags.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.Center;
            }
            return alignment;
        }

        public GravityFlags ToVerticalAlignment(object value, GravityFlags defaultAlignment = GravityFlags.Top)
        {
            if (value is GravityFlags)
            {
                return (GravityFlags)value;
            }

            GravityFlags alignment = defaultAlignment;
            string alignmentValue = ToString(value);
            if (alignmentValue == "Top")
            {
                alignment = GravityFlags.Top;
            }
            if (alignmentValue == "Bottom")
            {
                alignment = GravityFlags.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.Center;
            }
            return alignment;
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

        public void processThicknessProperty(JToken thicknessAttributeValue, ThicknessSetter thicknessSetter)
        {
            if (thicknessAttributeValue is Newtonsoft.Json.Linq.JValue)
            {
                processElementProperty((string)thicknessAttributeValue, value =>
                {
                    thicknessSetter.SetThickness((int)ToDeviceUnits(value));
                }, "0");
            }
            else if (thicknessAttributeValue is JObject)
            {
                JObject marginObject = thicknessAttributeValue as JObject;

                processElementProperty((string)marginObject.Property("left"), value =>
                {
                    thicknessSetter.SetThicknessLeft((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty((string)marginObject.Property("top"), value =>
                {
                    thicknessSetter.SetThicknessTop((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty((string)marginObject.Property("right"), value =>
                {
                    thicknessSetter.SetThicknessRight((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty((string)marginObject.Property("bottom"), value =>
                {
                    thicknessSetter.SetThicknessBottom((int)ToDeviceUnits(value));
                }, "0");
            }
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
            processElementProperty((string)controlSpec["opacity"], value => this.Control.Alpha = (float)ToDouble(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Visibility = ToBoolean(value) ? ViewStates.Visible : ViewStates.Gone);
            processElementProperty((string)controlSpec["enabled"], value => this.Control.Enabled = ToBoolean(value));

            processThicknessProperty(controlSpec["margin"], new MarginThicknessSetter(this));
            // Since some controls have to treat padding differently, the padding attribute is handled by the individual control classes
            // processThicknessProperty(controlSpec["padding"], new PaddingThicknessSetter(this.Control));

            if (!(this is AndroidBorderWrapper) && !(this is AndroidRectangleWrapper))
            {
                processElementProperty((string)controlSpec["background"], value => this.Control.SetBackgroundColor(ToColor(value)));
            }

            processFontAttribute(controlSpec, new AndroidFontSetter(this.Control));

            TextView textView = this.Control as TextView;
            if (textView != null)
            {
                processElementProperty((string)controlSpec["foreground"], value => textView.SetTextColor(ToColor(value)));
            }

            // These elements are very common among derived classes, so we'll do some runtime reflection...
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
                case "border":
                    controlWrapper = new AndroidBorderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new AndroidButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new AndroidCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new AndroidTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "image":
                    controlWrapper = new AndroidImageWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listbox":
                    controlWrapper = new AndroidListBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listview":
                    controlWrapper = new AndroidListViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new AndroidTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "picker":
                    controlWrapper = new AndroidPickerWrapper(parent, bindingContext, controlSpec);
                    break;
                case "rectangle":
                    controlWrapper = new AndroidRectangleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "scrollview":
                    controlWrapper = new AndroidScrollWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new AndroidSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new AndroidStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
                case "text":
                    controlWrapper = new AndroidTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toggle":
                    controlWrapper = new AndroidToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
                case "webview":
                    controlWrapper = new AndroidWebViewWrapper(parent, bindingContext, controlSpec);
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