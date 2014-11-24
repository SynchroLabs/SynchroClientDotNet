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
using Android.Util;
using Android.Graphics;

namespace SynchroClientAndroid.Controls
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

        // The SetTypeface method used below takes an "extra" stlye param, which is documented as:
        //
        //     "Sets the typeface and style in which the text should be displayed, and turns on the fake bold and italic bits
        //      in the Paint if the Typeface that you provided does not have all the bits in the style that you specified."
        //
        // When using this method with such a font (like the default system monospace font), this works fine unless you are
        // trying to set the style from any non-normal value to normal, in which case it fails to restore it to normal.
        // Not sure if this is an Android TextView bug or a Xamarin bug, but would guess the former.  Setting the typeface
        // to a face that does support the style bits, then setting it to the proper typeface with the extra style param
        // seems to work (and doesn't produce any visible flickering or other artifacts).  So we're going with that for now.
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
                // !!! These seem to be equivalent, but produce fonts that are larger than on other platforms (the glyph span is the specified height, 
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
                _control.LayoutParameters = layoutParams; // Required to trigger real-time update
            }
        }

        public override void SetThicknessTop(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.TopMargin = thickness;
                _control.LayoutParameters = layoutParams; // Required to trigger real-time update
            }
        }

        public override void SetThicknessRight(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.RightMargin = thickness;
                _control.LayoutParameters = layoutParams; // Required to trigger real-time update
            }
        }

        public override void SetThicknessBottom(int thickness)
        {
            ViewGroup.MarginLayoutParams layoutParams = _control.LayoutParameters as ViewGroup.MarginLayoutParams;
            if (layoutParams != null)
            {
                layoutParams.BottomMargin = thickness;
                _control.LayoutParameters = layoutParams; // Required to trigger real-time update
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
        static Logger logger = Logger.GetLogger("AndroidControlWrapper");

        protected View _control;
        public View Control { get { return _control; } }

        protected AndroidPageView _pageView;
        public AndroidPageView PageView { get { return _pageView; } }

        protected int _height = ViewGroup.LayoutParams.WrapContent;
        protected int _width = ViewGroup.LayoutParams.WrapContent;

        public AndroidControlWrapper(AndroidPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control) :
            base(stateManager, viewModel, bindingContext)
        {
            _pageView = pageView;
            _control = control;
        }

        public AndroidControlWrapper(ControlWrapper parent, BindingContext bindingContext) :
            base(parent, bindingContext)
        {
            _pageView = ((AndroidControlWrapper)parent).PageView;
        }

        public void InitializeLayoutParameters()
        {
            if (_isVisualElement && (_control.LayoutParameters == null))
            {
                _control.LayoutParameters = new ViewGroup.MarginLayoutParams(_width, _height);
            }
        }

        public void updateSize()
        {
            if (_isVisualElement)
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
        }

        public int Width 
        {
            get { return _width; }
            set 
            {
                _width = value;
                if (_width >= 0)
                {
                    _control.SetMinimumWidth(value);
                }
                this.updateSize();
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                if (_height >= 0)
                {
                    _control.SetMinimumHeight(value);
                }
                this.updateSize();
            }
        }

        public void AddToLinearLayout(ViewGroup layout, JObject childControlSpec)
        {
            LinearLayout.LayoutParams linearLayoutParams = this.Control.LayoutParameters as LinearLayout.LayoutParams;

            if (linearLayoutParams == null)
            {
                // Here we are essentially "upgrading" any current LayoutParams to LinearLayout.LayoutParams (if needed)
                //
                // The LinearLayout.LayoutParams constructor is too dumb to look at the class of the LayoutParams passed in, and
                // instead requires you to bind to the correct constructor variant based on the class of the provided layout params.
                //
                if (this.Control.LayoutParameters is ViewGroup.MarginLayoutParams)
                {
                    linearLayoutParams = new LinearLayout.LayoutParams((ViewGroup.MarginLayoutParams)this.Control.LayoutParameters);
                }
                else if (this.Control.LayoutParameters != null)
                {
                    linearLayoutParams = new LinearLayout.LayoutParams(this.Control.LayoutParameters);
                }
                else
                {
                    linearLayoutParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                }

                // The control might have had gravity (h/v alignment) set before getting added to the linear layout, and if so, we 
                // want to pick up those values in the new LayoutParams now...
                //
                linearLayoutParams.Gravity = VerticalAlignment | HorizontalAlignment;
            }

            int heightStarCount = GetStarCount((string)childControlSpec["height"]);
            int widthStarCount = GetStarCount((string)childControlSpec["width"]);

            Orientation orientation = Orientation.Horizontal;
            if (layout is LinearLayout)
            {
                orientation = ((LinearLayout)layout).Orientation;
            }
            else if (layout is FlowLayout)
            {
                orientation = ((FlowLayout)layout).Orientation;
            }

            if (orientation == Orientation.Horizontal)
            {
                if (heightStarCount > 0)
                {
                    linearLayoutParams.Height = LinearLayout.LayoutParams.MatchParent;
                }

                if (widthStarCount > 0)
                {
                    linearLayoutParams.Width = 0;
                    linearLayoutParams.Weight = widthStarCount;
                }
            }
            else // Orientation.Vertical
            {
                if (widthStarCount > 0)
                {
                    linearLayoutParams.Width = LinearLayout.LayoutParams.MatchParent;
                }
                
                if (heightStarCount > 0)
                {
                    linearLayoutParams.Height = 0;
                    linearLayoutParams.Weight = heightStarCount;
                }
            }

            this.Control.LayoutParameters = linearLayoutParams;

            layout.AddView(this.Control);
        }

        protected void updateGravity()
        {
            if (this.Control.LayoutParameters != null)
            {
                LinearLayout.LayoutParams linearLayoutParams = this.Control.LayoutParameters as LinearLayout.LayoutParams;
                if (linearLayoutParams != null)
                {
                    linearLayoutParams.Gravity = _horizontalAlignment | _verticalAlignment;
                    _control.RequestLayout();
                }
            }
        }

        protected GravityFlags _verticalAlignment = GravityFlags.Top;
        public GravityFlags VerticalAlignment
        {
            get { return _verticalAlignment; }
            set
            {
                _verticalAlignment = value;
                updateGravity();
            }
        }

        protected GravityFlags _horizontalAlignment = GravityFlags.Left;
        public GravityFlags HorizontalAlignment
        {
            get { return _horizontalAlignment; }
            set
            {
                _horizontalAlignment = value;
                updateGravity();
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
            else if (alignmentValue == "Right")
            {
                alignment = GravityFlags.Right;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.CenterHorizontal;
            }
            else if (alignmentValue == "Stretch")
            {
                alignment = GravityFlags.FillHorizontal;
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
            else if (alignmentValue == "Bottom")
            {
                alignment = GravityFlags.Bottom;
            }
            else if (alignmentValue == "Center")
            {
                alignment = GravityFlags.CenterVertical;
            }
            else if (alignmentValue == "Stretch")
            {
                alignment = GravityFlags.FillVertical;
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
            if (thicknessAttributeValue is MaaasCore.JValue)
            {
                processElementProperty(thicknessAttributeValue, value =>
                {
                    thicknessSetter.SetThickness((int)ToDeviceUnits(value));
                }, "0");
            }
            else if (thicknessAttributeValue is JObject)
            {
                JObject marginObject = thicknessAttributeValue as JObject;

                processElementProperty(marginObject.GetValue("left"), value =>
                {
                    thicknessSetter.SetThicknessLeft((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty(marginObject.GetValue("top"), value =>
                {
                    thicknessSetter.SetThicknessTop((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty(marginObject.GetValue("right"), value =>
                {
                    thicknessSetter.SetThicknessRight((int)ToDeviceUnits(value));
                }, "0");
                processElementProperty(marginObject.GetValue("bottom"), value =>
                {
                    thicknessSetter.SetThicknessBottom((int)ToDeviceUnits(value));
                }, "0");
            }
        }

        protected void setHeight(object value)
        {
            string heightString = ToString(value);
            if (heightString.IndexOf("*") >= 0)
            {
                logger.Debug("Got star height string: {0}", value);
                this.Height = ViewGroup.LayoutParams.MatchParent;
            }
            else
            {
                this.Height = (int)ToDeviceUnits(value);
            }
        }

        protected void setWidth(object value)
        {
            string widthString = ToString(value);
            if (widthString.IndexOf("*") >= 0)
            {
                logger.Debug("Got star width string: {0}", value);
                this.Width = ViewGroup.LayoutParams.MatchParent;
            }
            else
            {
                this.Width = (int)ToDeviceUnits(value);
            }
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            logger.Debug("Processing framework element properties");

            // !!! This could be a little more thourough ;)

            //processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));

            processElementProperty(controlSpec["horizontalAlignment"], value => this.HorizontalAlignment = ToHorizontalAlignment(value));
            processElementProperty(controlSpec["verticalAlignment"], value => this.VerticalAlignment = ToVerticalAlignment(value));

            processElementProperty(controlSpec["height"], value => setHeight(value));
            processElementProperty(controlSpec["width"], value => setWidth(value));
            updateSize(); // To init the layout params

            processElementProperty(controlSpec["minheight"], value => this.Control.SetMinimumHeight((int)ToDeviceUnits(value)));
            processElementProperty(controlSpec["minwidth"], value => this.Control.SetMinimumWidth((int)ToDeviceUnits(value)));

            //processElementProperty(controlSpec["maxheight"], value => this.Control.MaxHeight = ToDeviceUnits(value));
            //processElementProperty(controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDeviceUnits(value));

            processElementProperty(controlSpec["opacity"], value => this.Control.Alpha = (float)ToDouble(value));
            processElementProperty(controlSpec["visibility"], value => this.Control.Visibility = ToBoolean(value) ? ViewStates.Visible : ViewStates.Gone);
            processElementProperty(controlSpec["enabled"], value => this.Control.Enabled = ToBoolean(value));

            processThicknessProperty(controlSpec["margin"], new MarginThicknessSetter(this));
            // Since some controls have to treat padding differently, the padding attribute is handled by the individual control classes

            if (!(this is AndroidBorderWrapper) && !(this is AndroidRectangleWrapper))
            {
                processElementProperty(controlSpec["background"], value => this.Control.SetBackgroundColor(ToColor(value)));
            }

            processFontAttribute(controlSpec, new AndroidFontSetter(this.Control));

            TextView textView = this.Control as TextView;
            if (textView != null)
            {
                processElementProperty(controlSpec["foreground"], value => textView.SetTextColor(ToColor(value)));
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

        public static AndroidControlWrapper WrapControl(AndroidPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control)
        {
            return new AndroidControlWrapper(pageView, stateManager, viewModel, bindingContext, control);
        }

        public static AndroidControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            AndroidControlWrapper controlWrapper = null;

            switch ((string)controlSpec["control"])
            {
                case "actionBar.item":
                    controlWrapper = new AndroidActionWrapper(parent, bindingContext, controlSpec);
                    break;
                case "actionBar.toggle":
                    controlWrapper = new AndroidActionToggleWrapper(parent, bindingContext, controlSpec);
                    break;
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
                case "gridview":
                    controlWrapper = new AndroidGridViewWrapper(parent, bindingContext, controlSpec);
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
                case "location":
                    controlWrapper = new AndroidLocationWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new AndroidTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "picker":
                    controlWrapper = new AndroidPickerWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressbar":
                    controlWrapper = new AndroidSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressring":
                    controlWrapper = new AndroidProgressRingWrapper(parent, bindingContext, controlSpec);
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
                case "wrappanel":
                    controlWrapper = new AndroidWrapPanelWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                if (controlWrapper.Control != null)
                {
                    controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                }
                parent.ChildControls.Add(controlWrapper);
                if (controlWrapper.Control != null)
                {
                    controlWrapper.Control.Tag = new WrapperHolder(controlWrapper);
                }
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