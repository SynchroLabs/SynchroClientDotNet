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
    class iOSControlWrapper : ControlWrapper
    {
        protected UIView _control;
        public UIView Control { get { return _control; } }

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

        protected void applyFrameworkElementDefaults(UIView element)
        {
            // !!! This could be a little more thourough ;)
        }

        protected void processElementDimensions(JObject controlSpec, float defaultWidth = 100, float defaultHeight = 100)
        {
            this.Control.Frame = new RectangleF(0, 0, defaultWidth, defaultHeight);

            processElementProperty((string)controlSpec["height"], value => 
            {
                RectangleF frame = this.Control.Frame;
                frame.Height = (float)ToDouble(value);
                this.Control.Frame = frame;
            });

            processElementProperty((string)controlSpec["width"], value => 
            {
                RectangleF frame = this.Control.Frame;
                frame.Width = (float)ToDouble(value);
                this.Control.Frame = frame;
            });
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            // !!! This could be a little more thourough ;)
            Util.debug("Processing framework element properties");

            //processElementProperty((string)controlSpec["name"], value => this.Control.Name = ToString(value));
            //processElementProperty((string)controlSpec["height"], value => this.Control.Height = ToDouble(value));
            //processElementProperty((string)controlSpec["width"], value => this.Control.Width = ToDouble(value));
            //processElementProperty((string)controlSpec["minheight"], value => this.Control.MinHeight = ToDouble(value));
            //processElementProperty((string)controlSpec["minwidth"], value => this.Control.MinWidth = ToDouble(value));
            //processElementProperty((string)controlSpec["maxheight"], value => this.Control.MaxHeight = ToDouble(value));
            //processElementProperty((string)controlSpec["maxwidth"], value => this.Control.MaxWidth = ToDouble(value));
            //processElementProperty((string)controlSpec["opacity"], value => this.Control.Opacity = ToDouble(value));

            processElementProperty((string)controlSpec["visibility"], value => this.Control.Hidden = !ToBoolean(value));

            if (this.Control is UIControl)
            {
                processElementProperty((string)controlSpec["enabled"], value => ((UIControl)this.Control).Enabled = ToBoolean(value));
            }
            else
            {
                processElementProperty((string)controlSpec["enabled"], value => this.Control.UserInteractionEnabled = ToBoolean(value));
            }

            // !!! ((UIControl)this.Control).enabled
            //processMarginProperty(controlSpec["margin"]);

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            //
            // processElementProperty((string)controlSpec["fontsize"], value => textView.TextSize = (float)ToDouble(value) * 160/72);
            // processElementPropertyIfPresent((string)controlSpec["fontweight"], "FontWeight", value => ToFontWeight(value));
            //processElementPropertyIfPresent((string)controlSpec["background"], "Background", value => ToBrush(value));
            //processElementPropertyIfPresent((string)controlSpec["foreground"], "Foreground", value => ToBrush(value));
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
                case "text":
                    controlWrapper = new iOSTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                case "password":
                    controlWrapper = new iOSTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new iOSButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new iOSStackPanelWrapper(parent, bindingContext, controlSpec);
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