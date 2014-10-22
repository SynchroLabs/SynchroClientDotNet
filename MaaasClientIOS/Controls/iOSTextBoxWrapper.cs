using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace MaaasClientIOS.Controls
{
    class iOSTextBoxWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSTextBoxWrapper");

        bool _updateOnChange = false;

        public iOSTextBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating text box element with value of: " + controlSpec["value"]);

            UITextField textBox = new UITextField();
            this._control = textBox;

            if ((string)controlSpec["control"] == "password")
            {
                textBox.SecureTextEntry = true;
            }

            textBox.BorderStyle = UITextBorderStyle.RoundedRect;

            processElementDimensions(controlSpec, 100); // Default width of 100
            
            applyFrameworkElementDefaults(textBox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return textBox.Text; }, value => textBox.Text = ToString(value)))
            {
                processElementProperty((string)controlSpec["value"], value => textBox.Text = ToString(value));
                textBox.SizeToFit();
            }

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            processElementProperty((string)controlSpec["placeholder"], value => textBox.Placeholder = ToString(value));

            textBox.EditingChanged += textBox_EditingChanged;
        }

        void textBox_EditingChanged(object sender, EventArgs e)
        {
            var textBox = sender as UITextField;

            // Edit controls have a bad habit of posting a text changed event, and there are cases where 
            // this event is generated based on programmatic setting of text and comes in asynchronously
            // after that programmatic action, making it difficult to distinguish actual user changes.
            // This shortcut will help a lot of the time, but there are still cases where this will be 
            // signalled incorrectly (such as in the case where a control with focus is the target of
            // an update from the server), so we'll do some downstream delta checking as well, but this
            // check will cut down most of the chatter.
            //
            if (textBox.IsFirstResponder)
            {
                updateValueBindingForAttribute("value");
                if (_updateOnChange)
                {
                    Task t = this.StateManager.processUpdate();
                }
            }
        }
    }
}