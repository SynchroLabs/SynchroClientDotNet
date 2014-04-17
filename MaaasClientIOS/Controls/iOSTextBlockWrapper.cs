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
    class ResizableLabel : UILabel 
    {
        protected FrameProperties _frameProperties;
        protected SizeF _lastComputedSize;

        public ResizableLabel(FrameProperties frameProperties) : base()
        {
            _frameProperties = frameProperties;
            _lastComputedSize = new SizeF(0, 0);
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                this.UpdateSize();
            }
        }

        protected void UpdateComputedSize(SizeF size)
        {
            _lastComputedSize.Width = size.Width;
            _lastComputedSize.Height = size.Height;

            RectangleF frame = this.Frame;
            frame.Size = size;
            this.Frame = frame;
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if ((this.Frame.Size.Width != _lastComputedSize.Width) || (this.Frame.Size.Height != _lastComputedSize.Height))
            {
                // Util.debug("Resizable label - layoutSubviews() - new size - h: " + this.Frame.Size.Height + ", w: " + this.Frame.Size.Width);
                //
                this.UpdateSize();
            }
        }

        public void UpdateSize()
        {
            if ((_frameProperties.HeightSpec == SizeSpec.WrapContent) && (_frameProperties.WidthSpec == SizeSpec.WrapContent))
            {
                // If both dimensions are WrapContent, then we don't care what the current dimensions are, we just sizeToFit (note
                // that this will not do any line wrapping and will consume the width of the string as a single line).
                //
                this.Lines = 1;
                SizeF size = this.SizeThatFits(new SizeF(0, 0)); // Compute height and width
                this.UpdateComputedSize(size);
            }
            else if (_frameProperties.HeightSpec == SizeSpec.WrapContent)
            {
                // If only the height is WrapContent, then we obey the current width and set the height based on how tall the text would
                // be when wrapped at the current width.  
                //
                SizeF size = this.SizeThatFits(new SizeF(this.Frame.Size.Width, 0)); // Compute height
                size.Width = this.Frame.Size.Width; // Maintain width
                this.UpdateComputedSize(size);
            }
            else if (_frameProperties.WidthSpec == SizeSpec.WrapContent)
            {
                // If only the width is WrapContent then we'll get the maximum width assuming the text is on a single line and we'll 
                // set the width to that and leave the height alone (kind of a non-sensical case).
                //
                SizeF size = this.SizeThatFits(new SizeF(0, 0)); // Compute width
                size.Height = this.Frame.Height; // Maintain height
                this.UpdateComputedSize(size);
            }
        }
    }

    class TextBlockFontSetter : iOSFontSetter
    {
        ResizableLabel _label;

        public TextBlockFontSetter(ResizableLabel label)
            : base(label.Font)
        {
            _label = label;
        }

        public override void setFont(UIFont font)
        {
            _label.Font = font;
            _label.UpdateSize();
        }
    }

    class iOSTextBlockWrapper : iOSControlWrapper
    {
        public iOSTextBlockWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text block element with text of: " + controlSpec["value"]);

            ResizableLabel textBlock = new ResizableLabel(this.FrameProperties);
            textBlock.Lines = 0;
            textBlock.LineBreakMode = UILineBreakMode.WordWrap;

            this._control = textBlock;

            processElementDimensions(controlSpec, 0, 0);
            applyFrameworkElementDefaults(textBlock);

            processElementProperty((string)controlSpec["foreground"], value =>
            {
                ColorARGB colorArgb = ControlWrapper.getColor(ToString(value));
                UIColor color = UIColor.FromRGBA(colorArgb.r, colorArgb.g, colorArgb.b, colorArgb.a);
                textBlock.TextColor = color;
            });

            processFontAttribute(controlSpec, new TextBlockFontSetter(textBlock));

            processElementProperty((string)controlSpec["value"], value => 
            {
                textBlock.Text = ToString(value);
            });

            processElementProperty((string)controlSpec["textAlignment"], value =>
            {
                String alignString = ToString(value);
                if (alignString == "Left")
                {
                    textBlock.TextAlignment = UITextAlignment.Left;
                }
                if (alignString == "Center")
                {
                    textBlock.TextAlignment = UITextAlignment.Center;
                }
                else if (alignString == "Right")
                {
                    textBlock.TextAlignment = UITextAlignment.Right;
                }
            });
        }
    }
}