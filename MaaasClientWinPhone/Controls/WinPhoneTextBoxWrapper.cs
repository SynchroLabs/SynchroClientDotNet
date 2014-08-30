using MaaasCore;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneTextBoxWrapper : WinPhoneControlWrapper
    {
        bool _updateOnChange = false;

        public WinPhoneTextBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text box element with value of: " + controlSpec["value"]);

            // Switched to PhoneTextBox to get PlaceholderText functionality
            // TextBox textBox = new TextBox();
            PhoneTextBox textBox = new PhoneTextBox();

            this._control = textBox;

            applyFrameworkElementDefaults(textBox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return textBox.Text; }, value => textBox.Text = ToString(value)))
            {
                processElementProperty((string)controlSpec["value"], value => textBox.Text = ToString(value));
            }

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            processElementProperty((string)controlSpec["placeholder"], value => textBox.PlaceholderText = ToString(value));

            textBox.TextChanged += textBox_TextChanged;
        }

        void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            // Edit controls have a bad habit of posting a text changed event, and there are cases where 
            // this event is generated based on programmatic setting of text and comes in asynchronously
            // after that programmatic action, making it difficult to distinguish actual user changes.
            // This shortcut will help a lot of the time, but there are still cases where this will be 
            // signalled incorrectly (such as in the case where a control with focus is the target of
            // an update from the server), so we'll do some downstream delta checking as well, but this
            // check will cut down most of the chatter.
            //
            // !!! if (textBox.FocusState != FocusState.Unfocused)
            {
                updateValueBindingForAttribute("value");
                if (_updateOnChange)
                {
                    this.StateManager.processUpdate();
                }
            }
        }
    }
}

