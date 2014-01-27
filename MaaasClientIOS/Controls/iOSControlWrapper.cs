using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    public enum HorizontalAlignment : uint
    {
        UNDEFINED = 0,
        Center,
        Left,
        Right,
        Stretch
    }

    public enum VerticalAlignment : uint
    {
        UNDEFINED = 0,
        Center,
        Top,
        Bottom,
        Stretch
    }

    public enum Orientation : uint
    {
        Horizontal,
        Vertical
    }

    public enum SelectionMode : uint
    {
        None,
        Single,
        Multiple
    }

    public class FrameProperties
    {
        public bool WidthSpecified = false;
        public bool HeightSpecified = false;
    }

    public class iOSFontSetter : FontSetter
    {
        FontFaceType _faceType = FontFaceType.FONT_DEFAULT;
        bool _bold = false;
        bool _italic = false;
        float _size = 17.0f;

        public iOSFontSetter()
        {
            // !!! Ideally, it would be nice to be able to get the font name from the current font (the default)
            //     and to somehow use that unless SetFaceType gets called (so you can change just the size/style
            //     without having to actually pick a font).  Not sure how this would work, unless maybe there
            //     was a big data-driven function that took into consideration common fonts from iOS 6 and 7.
            //     This is complicated by the fact that for some fonts their "normal" weight is actually a very
            //     light weight (especially in iOS 7) and their bold weight is often normal or lighter.  So you
            //     kind of have to know each font and what you want to do for normal/bold/italic based on the
            //     name of the font (which will also include some of those modifiers).
            //
            // !!! See this for list of fonts by version: http://iosfonts.com/
            //
            // !!! Also, consider reviewing Pixtamatic font logic (enumerate/parse/catalog all fonts)
            //
        }

        public virtual void setFont(UIFont font);

        protected void createAndSetFont()
        {
            string faceName = null;

            switch (_faceType)
            {
                case FontFaceType.FONT_DEFAULT:
                    // !!! Get name, consider bold/italic
                    break;
                case FontFaceType.FONT_SANSERIF:
                    // !!! Get name, consider bold/italic
                    break;
                case FontFaceType.FONT_SERIF:
                    // !!! Get name, consider bold/italic
                    break;
                case FontFaceType.FONT_MONOSPACE:
                    // !!! Get name, consider bold/italic
                    break;
            }

            if (faceName != null)
            {
                UIFont font = UIFont.FromName(faceName, _size);
                this.setFont(font);
            }
        }

        public override void SetFaceType(FontFaceType faceType)
        {
            _faceType = faceType;
            this.createAndSetFont();
        }

        public override void SetSize(double size)
        {
            _size = (float)size;
            this.createAndSetFont();
        }


        public override void SetBold(bool bold)
        {
            _bold = bold;
            this.createAndSetFont();
        }

        public override void SetItalic(bool italic)
        {
            _italic = italic;
            this.createAndSetFont();
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
        protected iOSControlWrapper _controlWrapper;

        public MarginThicknessSetter(iOSControlWrapper controlWrapper)
        {
            _controlWrapper = controlWrapper;
        }

        public override void SetThicknessLeft(int thickness)
        {
            _controlWrapper.MarginLeft = thickness;
        }

        public override void SetThicknessTop(int thickness)
        {
            _controlWrapper.MarginTop = thickness;
        }

        public override void SetThicknessRight(int thickness)
        {
            _controlWrapper.MarginRight = thickness;
        }

        public override void SetThicknessBottom(int thickness)
        {
            _controlWrapper.MarginBottom = thickness;
        }
    }

    public class iOSControlWrapper : ControlWrapper
    {
        protected UIView _control;
        public UIView Control { get { return _control; } }

        protected UIEdgeInsets _margin = new UIEdgeInsets(0, 0, 0, 0);

        public iOSControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, UIView control) :
            base(stateManager, viewModel, bindingContext)
        {
            _control = control;
        }

        public iOSControlWrapper(ControlWrapper parent, BindingContext bindingContext, UIView control = null) :
            base(parent, bindingContext)
        {
            _control = control;
        }

        public UIEdgeInsets Margin
        {
            get { return _margin; }
            set
            {
                _margin = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        public float MarginLeft
        {
            get { return _margin.Left; }
            set
            {
                _margin.Left = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        public float MarginTop
        {
            get { return _margin.Top; }
            set
            {
                _margin.Top = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        public float MarginRight
        {
            get { return _margin.Right; }
            set
            {
                _margin.Right = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        public float MarginBottom
        {
            get { return _margin.Bottom; }
            set
            {
                _margin.Bottom = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
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

        public SelectionMode ToSelectionMode(object value, SelectionMode defaultSelectionMode = SelectionMode.Single)
        {
            if (value is SelectionMode)
            {
                return (SelectionMode)value;
            }

            SelectionMode selectionMode = defaultSelectionMode;
            string selectionModeValue = ToString(value);
            if (selectionModeValue == "None")
            {
                selectionMode = SelectionMode.None;
            }
            else if (selectionModeValue == "Single")
            {
                selectionMode = SelectionMode.Single;
            }
            else if (selectionModeValue == "Multiple")
            {
                selectionMode = SelectionMode.Multiple;
            }
            return selectionMode;
        }

        protected static UIColor ToColor(object value)
        {
            ColorARGB color = ControlWrapper.getColor(ToString(value));
            if (color != null)
            {
                return UIColor.FromRGBA(color.r, color.g, color.b, color.a);
            }
            else
            {
                return null;
            }
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

        protected void applyFrameworkElementDefaults(UIView element)
        {
            // !!! This could be a little more thourough ;)
        }

        protected FrameProperties processElementDimensions(JObject controlSpec, float defaultWidth = 100, float defaultHeight = 100)
        {
            FrameProperties frameProps = new FrameProperties();
            frameProps.HeightSpecified = ((string)controlSpec["height"] != null);
            frameProps.WidthSpecified = ((string)controlSpec["width"] != null);

            this.Control.Frame = new RectangleF(0, 0, defaultWidth, defaultHeight);

            processElementProperty((string)controlSpec["height"], value => 
            {
                RectangleF frame = this.Control.Frame;
                frame.Height = (float)ToDeviceUnits(value);
                this.Control.Frame = frame;
                if (this.Control.Superview != null)
                {
                    this.Control.Superview.SetNeedsLayout();
                }
            });

            processElementProperty((string)controlSpec["width"], value => 
            {
                RectangleF frame = this.Control.Frame;
                frame.Width = (float)ToDeviceUnits(value);
                this.Control.Frame = frame;
                if (this.Control.Superview != null)
                {
                    this.Control.Superview.SetNeedsLayout();
                }
            });

            return frameProps;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            // !!! This could be a little more thourough ;)
            Util.debug("Processing framework element properties");

            //processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));
            //processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = ToDouble(value));
            //processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = ToDouble(value));
            //processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDouble(value));
            //processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDouble(value));

            processElementProperty((string)controlSpec["opacity"], value => this.Control.Layer.Opacity = (float)ToDouble(value));

            processElementProperty((string)controlSpec["background"], value => this.Control.BackgroundColor = ToColor(value));
            processElementProperty((string)controlSpec["visibility"], value => this.Control.Hidden = !ToBoolean(value));

            if (this.Control is UIControl)
            {
                processElementProperty((string)controlSpec["enabled"], value => ((UIControl)this.Control).Enabled = ToBoolean(value));
            }
            else
            {
                processElementProperty((string)controlSpec["enabled"], value => this.Control.UserInteractionEnabled = ToBoolean(value));
            }

            processThicknessProperty(controlSpec["margin"], new MarginThicknessSetter(this));

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            //
            // processElementProperty((string)controlSpec["fontsize"], value => textView.TextSize = (float)ToDouble(value) * 160/72);
            // processElementPropertyIfPresent((string)controlSpec["fontweight"], "FontWeight", value => ToFontWeight(value));
            // processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => ToBrush(value));
        }

        public iOSControlWrapper getChildControlWrapper(UIView control)
        {
            // Find the child control wrapper whose control matches the supplied value...
            foreach (iOSControlWrapper child in this.ChildControls)
            {
                if (child.Control == control)
                {
                    return child;
                }
            }

            return null;
        }

        public static iOSControlWrapper WrapControl(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, UIView control)
        {
            return new iOSControlWrapper(stateManager, viewModel, bindingContext, control);
        }

        public static iOSControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            iOSControlWrapper controlWrapper = null;

            switch ((string)controlSpec["type"])
            {
                case "border":
                    controlWrapper = new iOSBorderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new iOSButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "canvas":
                    controlWrapper = new iOSCanvasWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new iOSTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "image":
                    controlWrapper = new iOSImageWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listbox":
                    controlWrapper = new iOSListBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "listview":
                    controlWrapper = new iOSListViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new iOSTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "pickerview":
                    controlWrapper = new iOSPickerWrapper(parent, bindingContext, controlSpec);
                    break;
                case "rectangle":
                    controlWrapper = new iOSRectangleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "scrollview":
                    controlWrapper = new iOSScrollWrapper(parent, bindingContext, controlSpec);
                    break;
                case "slider":
                    controlWrapper = new iOSSliderWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new iOSStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
                case "text":
                    controlWrapper = new iOSTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toggle":
                    controlWrapper = new iOSToggleSwitchWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, iOSControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) =>
            {
                iOSControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
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