using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SynchroCore;
using System.Threading.Tasks;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    class ToggleSwitchView : PaddedView
    {
        static Logger logger = Logger.GetLogger("ToggleSwitchView");

        protected iOSControlWrapper _controlWrapper;
        protected UILabel _label;
        protected UISwitch _switch;
        protected int _spacing = 10;

        public ToggleSwitchView(iOSControlWrapper controlWrapper)
            : base()
        {
            _controlWrapper = controlWrapper;
        }

        public override SizeF IntrinsicContentSize
        {
            get
            {
                // Compute the "wrap contents" (minimum) size for our contents.
                //
                SizeF intrinsicSize = new SizeF(0, 0);

                if (_label != null)
                {
                    _label.SizeToFit();
                    intrinsicSize.Height = _label.Frame.Height;
                    intrinsicSize.Width = _label.Frame.Width;
                }

                if (_switch != null)
                {
                    intrinsicSize.Height = Math.Max(intrinsicSize.Height, _switch.Frame.Height);
                    intrinsicSize.Width += _switch.Frame.Width;
                    if (_label != null)
                    {
                        intrinsicSize.Width += _spacing;
                    }
                }
                return intrinsicSize;
            }
        }

        public override void AddSubview(UIView view)
        {
            if (view is UILabel)
            {
                _label = view as UILabel;
            }
            else if (view is UISwitch)
            {
                _switch = view as UISwitch;
            }
            else
            {
                // We're the only ones who call this, so this should never happen...
                throw new ArgumentException("Can only add UILabel or UISwitch");
            }

            base.AddSubview(view);
        }

        public override void LayoutSubviews()
        {
            // Util.debug("ToggleSwitchView - Layout subviews");

            base.LayoutSubviews();

            if (((_controlWrapper.FrameProperties.HeightSpec == SizeSpec.FillParent) && (this.Frame.Height == 0)) ||
                ((_controlWrapper.FrameProperties.WidthSpec == SizeSpec.FillParent) && (this.Frame.Width == 0)))
            {
                // If either dimension is star sized, and the current size in that dimension is zero, then we
                // can't layout our children (we have no space to lay them out in anyway).  So this is a noop.
                //
                return;
            }

            SizeF contentSize = this.IntrinsicContentSize;
            if (_controlWrapper.FrameProperties.HeightSpec != SizeSpec.WrapContent)
            {
                contentSize.Height = this.Frame.Height;
            }
            if (_controlWrapper.FrameProperties.WidthSpec != SizeSpec.WrapContent)
            {
                contentSize.Width = this.Frame.Width;
            }

            // Arrange the subviews (align as appropriate)
            //
            if (_label != null)
            {
                // !!! If the container is not wrap width, then we need to make sure the switch has the
                //     room it needs and the label formats itself into whatever width is left over.  Not
                //     sure if it would be better to wrap or ellipsize the label if it overflows.  See 
                //     iOSTextBlockWrapper for examples of size management.
                //

                // Left aligned, verticaly centered
                _label.SizeToFit();
                RectangleF labelFrame = _label.Frame;
                labelFrame.X = _padding.Left;
                labelFrame.Y = ((contentSize.Height - labelFrame.Height) / 2);
                _label.Frame = labelFrame;
            }

            if (_switch != null)
            {
                // Left aligned, vertically centered
                RectangleF switchFrame = _switch.Frame;
                switchFrame.X = _padding.Left;
                switchFrame.Y = ((contentSize.Height - switchFrame.Height) / 2);
                if (_label != null)
                {
                    // Right aligned
                    switchFrame.X = contentSize.Width - switchFrame.Width - _padding.Right;
                }
                _switch.Frame = switchFrame;
            }

            SizeF newPanelSize = new SizeF(0, 0);
            newPanelSize.Height += _switch.Frame.Bottom + _padding.Bottom;
            newPanelSize.Width += _switch.Frame.Right + _padding.Right;

            // Resize the stackpanel to contain the subview, as needed
            //

            // See if the stack panel might have changed size (based on content)
            //
            if ((_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent) || (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent))
            {
                SizeF panelSize = this.Frame.Size;
                if (_controlWrapper.FrameProperties.HeightSpec == SizeSpec.WrapContent)
                {
                    panelSize.Height = newPanelSize.Height;
                }
                if (_controlWrapper.FrameProperties.WidthSpec == SizeSpec.WrapContent)
                {
                    panelSize.Width = newPanelSize.Width;
                }

                // Only re-size and request superview layout if the size actually changes
                //
                if ((panelSize.Width != this.Frame.Width) || (panelSize.Height != this.Frame.Height))
                {
                    RectangleF panelFrame = this.Frame;
                    panelFrame.Size = panelSize;
                    this.Frame = panelFrame;
                    if (this.Superview != null)
                    {
                        this.Superview.SetNeedsLayout();
                    }
                }
            }
        }
    }

    class ToggleLabelFontSetter : iOSFontSetter
    {
        UILabel _label;

        public ToggleLabelFontSetter(UILabel label)
            : base(label.Font)
        {
            _label = label;
        }

        public override void setFont(UIFont font)
        {
            _label.Font = font;
            if (_label.Superview != null)
            {
                _label.Superview.SetNeedsLayout();
            }
        }
    }

    class iOSToggleSwitchWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSToggleSwitchWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle.Attribute };

        public iOSToggleSwitchWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating toggle switch element");

            UILabel label = new UILabel();
            UISwitch toggleSwitch = new UISwitch();

            ToggleSwitchView view = new ToggleSwitchView(this);
            view.AddSubview(label);
            view.AddSubview(toggleSwitch);

            this._control = view;

            processElementDimensions(controlSpec, 150, 50);

            applyFrameworkElementDefaults(this._control);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            // Switch
            //
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new JValue(toggleSwitch.On); }, value => toggleSwitch.On = ToBoolean(value)))
            {
                processElementProperty(controlSpec["value"], value => toggleSwitch.On = ToBoolean(value));
            }

            // There  is no straighforward way to change the labels on the switch itself (you can use custom images,
            // and people have tried hacking in to the view heirarchy to find the labels, but that changes between
            // iOS versions and is not considered a viable approach).  For now, we don't support this on iOS.
            //
            // !!! processElementProperty(controlSpec["onLabel"], value => toggleSwitch.TextOn = ToString(value));
            // !!! processElementProperty(controlSpec["offLabel"], value => toggleSwitch.TextOff = ToString(value));

            // Label
            //
            processElementProperty(controlSpec["caption"], value => label.Text = ToString(value));

            processElementProperty(controlSpec["foreground"], value =>
            {
                ColorARGB colorArgb = ControlWrapper.getColor(ToString(value));
                UIColor color = UIColor.FromRGBA(colorArgb.r, colorArgb.g, colorArgb.b, colorArgb.a);
                label.TextColor = color;
            });

            processFontAttribute(controlSpec, new ToggleLabelFontSetter(label));

            toggleSwitch.ValueChanged += toggleSwitch_ValueChanged;

            view.LayoutSubviews();
        }

        async void toggleSwitch_ValueChanged(object sender, EventArgs e)
        {
            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                logger.Debug("ToggleSwitch toggled with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}