using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.RegularExpressions;

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

    public enum SizeSpec : uint
    {
        WrapContent,
        Explicit,
        FillParent
    }

    public class FrameProperties
    {
        public SizeSpec WidthSpec = SizeSpec.WrapContent;
        public SizeSpec HeightSpec = SizeSpec.WrapContent;

        public int StarWidth = 0;
        public int StarHeight = 0;
    }

    //
    // Font stuff...
    //

    public enum FontSlope
    {
        Roman = 0, // Also Regular, Plain - standard upright font
        Italic,    // Italic font
        Oblique,   // Also Incline, Inclined - Slanted version of Roman glyphs
        Cursive    // Also Kursiv - Italic with cursive glyph connections
    }

    public enum FontWidth
    {
        Normal = 0,
        Narrow,    // Compressed, Condensed, Narrow
        Wide       // Wide, Extended, Expanded
    }

    public enum FontWeight
    {
        ExtraLight = 100, // ExtraLight or UltraLight
        Light      = 200, // Light or Thin
        Book       = 300, // Book or Demi
        Normal     = 400, // Normal or Regular
        Medium     = 500, // Medium
        Semibold   = 600, // Semibold, Demibold
        Bold       = 700, // Bold
        Black      = 800, // Black, ExtraBold or Heavy
        ExtraBlack = 900, // ExtraBlack, Fat, Poster or UltraBlack
    }

    public class FontMetrics
    {
        string _faceName;

        FontSlope _slope = FontSlope.Roman;
        FontWidth _width = FontWidth.Normal;
        FontWeight _weight = FontWeight.Normal;

        private static Regex _slope_italic = new Regex(@"Italic");
        private static Regex _slope_oblique = new Regex(@"Oblique|Incline");
        private static Regex _slope_cursive = new Regex(@"Cursive|Kursiv");

        private static Regex _width_narrow = new Regex(@"Compressed|Condensed|Narrow");
        private static Regex _width_wide = new Regex(@"Wide|Extended|Expanded");

        private static Regex _weight_100 = new Regex(@"ExtraLight|UltraLight");
        private static Regex _weight_200 = new Regex(@"Light|Thin");
        private static Regex _weight_300 = new Regex(@"Book|Demi");
        private static Regex _weight_400 = new Regex(@"Normal|Regular");
        private static Regex _weight_500 = new Regex(@"Medium");
        private static Regex _weight_600 = new Regex(@"Semibold|Demibold");
        private static Regex _weight_700 = new Regex(@"Bold");
        private static Regex _weight_800 = new Regex(@"Black|ExtraBold|Heavy");
        private static Regex _weight_900 = new Regex(@"ExtraBlack|Fat|Poster|UltraBlack");

        // The function of this class is to parse the font properties (slope/weight/width) from the font names, as
        // that's really the only indication that iOS gives us about the font metrics.
        //
        public FontMetrics(string faceName)
        {
            _faceName = faceName;

            if (_slope_italic.IsMatch(_faceName))
            {
                _slope = FontSlope.Italic;
            }
            else if (_slope_oblique.IsMatch(_faceName))
            {
                _slope = FontSlope.Oblique;
            }
            else if (_slope_cursive.IsMatch(_faceName))
            {
                _slope = FontSlope.Cursive;
            }

            if (_width_narrow.IsMatch(_faceName))
            {
                _width = FontWidth.Narrow;
            }
            else if (_width_wide.IsMatch(_faceName))
            {
                _width = FontWidth.Wide;
            }

            // The ordering below might look a little strange, but it is important.  We have to be careful not to match Light, Bold, Black,
            // or Demi in other stlyes (UltraLight, SemiBold, UltraBlack, etc), so we have to search for the longer terms first.
            //
            if (_weight_100.IsMatch(_faceName))
            {
                _weight = FontWeight.ExtraLight;
            }
            else if (_weight_400.IsMatch(_faceName))
            {
                _weight = FontWeight.Normal;
            }
            else if (_weight_500.IsMatch(_faceName))
            {
                _weight = FontWeight.Medium;
            }
            else if (_weight_900.IsMatch(_faceName))
            {
                _weight = FontWeight.ExtraBlack;
            }
            else if (_weight_800.IsMatch(_faceName))
            {
                _weight = FontWeight.Black;
            }
            else if (_weight_600.IsMatch(_faceName))
            {
                _weight = FontWeight.Semibold;
            }
            else if (_weight_700.IsMatch(_faceName))
            {
                _weight = FontWeight.Bold;
            }
            else if (_weight_200.IsMatch(_faceName))
            {
                _weight = FontWeight.Light;
            }
            else if (_weight_300.IsMatch(_faceName))
            {
                _weight = FontWeight.Book;
            }
        }

        public string Name { get { return _faceName; } }
        public FontSlope Slope { get { return _slope; } }
        public FontWidth Width { get { return _width; } }
        public FontWeight Weight { get { return _weight; } }

        // The math here works more or less as follows.  For each of the three criteria, a value of 1.0 is
        // given for a perfect match, a value of 0.8 is given for a "close" match, and a value of 0.5 is given
        // for a poor (typically opposite) match.  For font weight a sliding scale is used, but it more or less
        // matches up to the fixed scale values in the other metrics.  The overall match quality returned is the
        // product of these values.  A perfect match is 1.0, and the worst possible match is 0.125.  Importantly, 
        // a font that matches perfectly on two criteria, but opposite on the third, will score a 0.5, whereas a
        // font that is a close match (but not perfect) on all three criteria will score a 0.512 (it is considered
        // a better match).
        //
        public float MatchQuality(FontSlope slope, FontWeight weight, FontWidth width)
        {
            float matchQuality = 1;

            if (slope != _slope)
            {
                if ((slope != FontSlope.Roman) && (_slope != FontSlope.Roman))
                {
                    // Slopes aren't equal, but are both non-Roman, which is kind of close...
                    matchQuality *= 0.8f;
                }
                else
                {
                    // Slopes differ (one is Roman, one is some kind of non-Roman)...
                    matchQuality *= 0.5f;
                }
            }

            if (width != _width)
            {
                if ((width == FontWidth.Normal) || (_width == FontWidth.Normal))
                {
                    // Font widths are within one (either, but not both, are normal), which is kind of close...
                    matchQuality *= 0.8f;
                }
                else
                {
                    // The widths are opposite...
                    matchQuality *= 0.5f;
                }
            }

            if (weight != _weight)
            {
                int weightDifference = Math.Abs((int)weight - (int)_weight);
                // Max weight difference is 800 - We want to scale match from 1.0 (exact match) to 0.5 (opposite, or 800 difference)
                matchQuality *= (1.0f - (weightDifference / 1600f));
            }

            return matchQuality;
        }

        public override string ToString() 
        {
            return "FontMetrics - Face: " + _faceName + ", Weight: " + _weight + ", Slope: " + _slope + ", Width: " + _width;
        }
    }

    public abstract class FontFamily
    {
        public abstract UIFont CreateFont(bool bold, bool italic, float size);
    }

    public class FontFamilyFromName : FontFamily
    {
        string _familyName;
        List<FontMetrics> _fonts = new List<FontMetrics>();

        protected FontMetrics _plainFont;
        protected FontMetrics _boldFont;
        protected FontMetrics _italicFont;
        protected FontMetrics _boldItalicFont;

        public FontFamilyFromName(string familyName)
        {
            _familyName = familyName;
            string[] fontNames = UIFont.FontNamesForFamilyName(_familyName);
            foreach (string fontName in fontNames)
            {
                _fonts.Add(new FontMetrics(fontName));
            }

            _plainFont = GetBestMatch(FontSlope.Roman, FontWeight.Normal, FontWidth.Normal);
            _boldFont = GetBestMatch(FontSlope.Roman, FontWeight.Bold, FontWidth.Normal);
            _italicFont = GetBestMatch(FontSlope.Italic, FontWeight.Normal, FontWidth.Normal);
            _boldItalicFont = GetBestMatch(FontSlope.Italic, FontWeight.Bold, FontWidth.Normal);
        }
        
        public FontMetrics GetBestMatch(FontSlope slope, FontWeight weight, FontWidth width)
        {
            FontMetrics bestMatch = null;
            float bestMatchScore = -1;

            foreach(FontMetrics fontMetrics in _fonts)
            {
                float matchScore = fontMetrics.MatchQuality(slope, weight, width);
                if (matchScore > bestMatchScore)
                {
                    bestMatch = fontMetrics;
                    bestMatchScore = matchScore;
                }

                if (matchScore == 1)
                    break;
            }

            return bestMatch;
        }

        public override UIFont CreateFont(bool bold, bool italic, float size)
        {
            if (bold && italic)
            {
                return UIFont.FromName(_boldItalicFont.Name, size);
            }
            else if (bold)
            {
                return UIFont.FromName(_boldFont.Name, size);
            }
            else if (italic)
            {
                return UIFont.FromName(_italicFont.Name, size);
            }
            else
            {
                return UIFont.FromName(_plainFont.Name, size);
            }
        }
    }

    public class SystemFontFamily : FontFamily
    {
        public SystemFontFamily()
        {
        }

        public static bool IsStystemFont(UIFont font)
        {
            float currSize = font.PointSize;

            UIFont systemFont = UIFont.SystemFontOfSize(currSize);
            UIFont systemBoldFont = UIFont.BoldSystemFontOfSize(currSize);
            UIFont systemItalicFont = UIFont.ItalicSystemFontOfSize(currSize);

            return ((font == systemFont) || (font == systemBoldFont) || (font == systemItalicFont));
        }

        public override UIFont CreateFont(bool bold, bool italic, float size)
        {
            if (bold && italic)
            {
                // Family for system fonts: ".Helvetica NeueUI"
                //
                //   SystemFont        ".HelveticaNeueUI" 
                //   BoldSystemFont    ".HelveticaNeueUI-Bold"
                //   ItalicSystemFont  ".HelveticaNeueUI-Italic"
                //
                // There is no built-in way to get the system bold+italic font, and it cannot be enumerated from the system font family,
                // but it happens that you can create it explicitly if you know the name (which should generally be the same as the bold
                // name plus "Italic", but could be different if there was a different system font).
                //
                //   ".HelveticaNeueUI-BoldItalic" - Works
                //
                UIFont boldFont = UIFont.BoldSystemFontOfSize(size);
                UIFont boldItalicFont = UIFont.FromName(boldFont.Name + "Italic", size);
                if (boldItalicFont != null)
                {
                    return boldItalicFont;
                }
                else
                {
                    // If we can't create the bold+italic, we'll just return the bold (best we can do)
                    return boldFont;
                }
            }
            else if (bold)
            {
                return UIFont.BoldSystemFontOfSize(size);
            }
            else if (italic)
            {
                return UIFont.ItalicSystemFontOfSize(size);
            }
            else
            {
                return UIFont.SystemFontOfSize(size);
            }
        }
    }

    public abstract class iOSFontSetter : FontSetter
    {
        FontFamily _family = null;
        bool _bold = false;
        bool _italic = false;
        float _size = 17.0f;

        public iOSFontSetter(UIFont font)
        {
            if (SystemFontFamily.IsStystemFont(font))
            {
                _family = new SystemFontFamily();
            }
            else
            {
                _family = new FontFamilyFromName(font.FamilyName);
            }

            _size = font.PointSize;
        }

        public abstract void setFont(UIFont font);

        protected void createAndSetFont()
        {
            if (_family != null)
            {
                this.setFont(_family.CreateFont(_bold, _italic, _size));
            }
        }

        public override void SetFaceType(FontFaceType faceType)
        {
            // See this for list of iOS fonts by version: http://iosfonts.com/
            //
            // If the face type is set, then we will create a font family to use.  Otherwise, we'll fall back to
            // the family created in the constructor (based on the initial/existing font).
            //
            switch (faceType)
            {
                case FontFaceType.FONT_DEFAULT:
                    _family = new SystemFontFamily();
                    break;
                case FontFaceType.FONT_SANSERIF:
                    _family = new FontFamilyFromName("Helvetica Neue");
                    break;
                case FontFaceType.FONT_SERIF:
                    _family = new FontFamilyFromName("Times New Roman");
                    break;
                case FontFaceType.FONT_MONOSPACE:
                    _family = new FontFamilyFromName("Courier New");
                    break;
            }

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
        static Logger logger = Logger.GetLogger("iOSControlWrapper");

        protected UIView _control;
        public UIView Control { get { return _control; } }

        protected iOSPageView _pageView;
        public iOSPageView PageView { get { return _pageView; } }

        protected UIEdgeInsets _margin = new UIEdgeInsets(0, 0, 0, 0);

        public FrameProperties FrameProperties = new FrameProperties();

        protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
        public HorizontalAlignment HorizontalAlignment
        {
            get { return _horizontalAlignment; }
            set
            {
                _horizontalAlignment = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
        public VerticalAlignment VerticalAlignment
        {
            get { return _verticalAlignment; }
            set
            {
                _verticalAlignment = value;
                if (_control.Superview != null)
                {
                    _control.Superview.SetNeedsLayout();
                }
            }
        }

        public iOSControlWrapper(iOSPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, UIView control) :
            base(stateManager, viewModel, bindingContext)
        {
            _pageView = pageView;
            _control = control;
        }

        public iOSControlWrapper(ControlWrapper parent, BindingContext bindingContext, UIView control = null) :
            base(parent, bindingContext)
        {
            _pageView = ((iOSControlWrapper)parent).PageView;
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
                });
            }
            else if (thicknessAttributeValue is JObject)
            {
                JObject marginObject = thicknessAttributeValue as JObject;

                processElementProperty((string)marginObject.Property("left"), value =>
                {
                    thicknessSetter.SetThicknessLeft((int)ToDeviceUnits(value));
                });
                processElementProperty((string)marginObject.Property("top"), value =>
                {
                    thicknessSetter.SetThicknessTop((int)ToDeviceUnits(value));
                });
                processElementProperty((string)marginObject.Property("right"), value =>
                {
                    thicknessSetter.SetThicknessRight((int)ToDeviceUnits(value));
                });
                processElementProperty((string)marginObject.Property("bottom"), value =>
                {
                    thicknessSetter.SetThicknessBottom((int)ToDeviceUnits(value));
                });
            }
        }

        protected void applyFrameworkElementDefaults(UIView element, bool applyMargins = true)
        {
            // !!! This could be a little more thourough ;)

            if (applyMargins)
            {
                this.MarginLeft = (float)ToDeviceUnits(10);
                this.MarginTop = (float)ToDeviceUnits(10);
                this.MarginRight = (float)ToDeviceUnits(10);
                this.MarginBottom = (float)ToDeviceUnits(10);
            }
        }

        protected SizeF SizeThatFits(SizeF size)
        {
            SizeF sizeThatFits = new SizeF(size); // Default to size given ("fill parent")

            if ((this.FrameProperties.HeightSpec == SizeSpec.WrapContent) && (this.FrameProperties.WidthSpec == SizeSpec.WrapContent))
            {
                // If both dimensions are WrapContent, then we want to make the control as small as possible in both dimensions, without
                // respect to how big the client would like to make it.
                //
                sizeThatFits = this.Control.SizeThatFits(new SizeF(0, 0)); // Compute height and width
            }
            else if (this.FrameProperties.HeightSpec == SizeSpec.WrapContent)
            {
                // If only the height is WrapContent, then we obey the current width and attempt to compute the height.
                //
                sizeThatFits = this.Control.SizeThatFits(new SizeF(this.Control.Frame.Size.Width, 0)); // Compute height
                sizeThatFits.Width = this.Control.Frame.Size.Width; // Maintain width
            }
            else if (this.FrameProperties.WidthSpec == SizeSpec.WrapContent)
            {
                // If only the width is WrapContent, then we obey the current hiights and attempt to compute the width.
                //
                sizeThatFits = this.Control.SizeThatFits(new SizeF(0, this.Control.Frame.Size.Height)); // Compute width
                sizeThatFits.Height = this.Control.Frame.Height; // Maintain height
            }
            else // No content wrapping in either dimension...
            {
                if (this.FrameProperties.HeightSpec != SizeSpec.FillParent)
                {
                    sizeThatFits.Height = this.Control.Frame.Height;
                }
                if (this.FrameProperties.WidthSpec != SizeSpec.FillParent)
                {
                    sizeThatFits.Width = this.Control.Frame.Width;
                }                
            }

            return sizeThatFits;
        }

        protected void SizeToFit()
        {
            SizeF size = this.SizeThatFits(new SizeF(0, 0));
            RectangleF frame = this.Control.Frame;
            frame.Size = size;
            this.Control.Frame = frame;
        }

        protected FrameProperties processElementDimensions(JObject controlSpec, float defaultWidth = 0, float defaultHeight = 0)
        {
            if (defaultWidth == 0)
            {
                defaultWidth = this.Control.IntrinsicContentSize.Width;
                if (defaultWidth == -1)
                {
                    defaultWidth = 0;
                }
            }
            if (defaultHeight == 0)
            {
                defaultHeight = this.Control.IntrinsicContentSize.Height;
                if (defaultHeight == -1)
                {
                    defaultHeight = 0;
                }
            }

            this.Control.Frame = new RectangleF(0, 0, defaultWidth, defaultHeight);

            // Process star sizing...
            //
            int heightStarCount = GetStarCount((string)controlSpec["height"]);
            if (heightStarCount > 0)
            {
                this.FrameProperties.HeightSpec = SizeSpec.FillParent;
                this.FrameProperties.StarHeight = heightStarCount;
            }
            else
            {
                if ((string)controlSpec["height"] != null)
                {
                    this.FrameProperties.HeightSpec = SizeSpec.Explicit;
                }
                processElementProperty((string)controlSpec["height"], value =>
                {
                    RectangleF frame = this.Control.Frame;
                    frame.Height = (float)ToDeviceUnits(value);
                    this.Control.Frame = frame;
                    if (this.Control.Superview != null)
                    {
                        this.Control.Superview.SetNeedsLayout();
                    }
                    //this.SizeToFit();
                });
            }

            int widthStarCount = GetStarCount((string)controlSpec["width"]);
            if (widthStarCount > 0)
            {
                this.FrameProperties.WidthSpec = SizeSpec.FillParent;
                this.FrameProperties.StarWidth = widthStarCount; 
            }
            else
            {
                if ((string)controlSpec["width"] != null)
                {
                    this.FrameProperties.WidthSpec = SizeSpec.Explicit;
                }
                processElementProperty((string)controlSpec["width"], value =>
                {
                    RectangleF frame = this.Control.Frame;
                    frame.Width = (float)ToDeviceUnits(value);
                    this.Control.Frame = frame;
                    if (this.Control.Superview != null)
                    {
                        this.Control.Superview.SetNeedsLayout();
                    }
                    //this.SizeToFit();
                });
            }

            return this.FrameProperties;
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            logger.Debug("Processing framework element properties");

            // !!! This could be a little more thourough ;)

            //processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));
            //processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = ToDeviceUnits(value));
            //processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = ToDeviceUnits(value));
            //processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDeviceUnits(value));
            //processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDeviceUnits(value));

            processElementProperty((string)controlSpec["horizontalAlignment"], value => this.HorizontalAlignment = ToHorizontalAlignment(value));
            processElementProperty((string)controlSpec["verticalAlignment"], value => this.VerticalAlignment = ToVerticalAlignment(value));

            processElementProperty((string)controlSpec["opacity"], value => this.Control.Layer.Opacity = (float)ToDouble(value));

            processElementProperty((string)controlSpec["background"], value => this.Control.BackgroundColor = ToColor(value));
            processElementProperty((string)controlSpec["visibility"], value => 
            {
                this.Control.Hidden = !ToBoolean(value);
                if (this.Control.Superview != null)
                {
                    this.Control.Superview.SetNeedsLayout();
                }
            });

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

        public static iOSControlWrapper WrapControl(iOSPageView pageView, StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, UIView control)
        {
            return new iOSControlWrapper(pageView, stateManager, viewModel, bindingContext, control);
        }

        public static iOSControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            iOSControlWrapper controlWrapper = null;

            switch ((string)controlSpec["control"])
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
                case "gridview":
                    controlWrapper = new iOSGridViewWrapper(parent, bindingContext, controlSpec);
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
                case "navBar.button":
                    controlWrapper = new iOSToolBarWrapper(parent, bindingContext, controlSpec);
                    break;
                case "navBar.toggle":
                    controlWrapper = new iOSToolBarToggleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "password":
                    controlWrapper = new iOSTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "picker":
                    controlWrapper = new iOSPickerWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressbar":
                    controlWrapper = new iOSProgressBarWrapper(parent, bindingContext, controlSpec);
                    break;
                case "progressring":
                    controlWrapper = new iOSProgressRingWrapper(parent, bindingContext, controlSpec);
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
                case "toolBar.button":
                    controlWrapper = new iOSToolBarWrapper(parent, bindingContext, controlSpec);
                    break;
                case "toolBar.toggle":
                    controlWrapper = new iOSToolBarToggleWrapper(parent, bindingContext, controlSpec);
                    break;
                case "webview":
                    controlWrapper = new iOSWebViewWrapper(parent, bindingContext, controlSpec);
                    break;
                case "wrappanel":
                    controlWrapper = new iOSWrapPanelWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                if (controlWrapper.Control != null)
                {
                    controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                }
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