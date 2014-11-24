using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinTextBoxWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinTextBoxWrapper");

        bool _updateOnChange = false;

        public WinTextBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating text box element with value of: {0}", controlSpec["value"]);
            TextBox textBox = new TextBox();
            this._control = textBox;

            applyFrameworkElementDefaults(textBox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return textBox.Text; }, value => textBox.Text = ToString(value)))
            {
                processElementProperty(controlSpec["value"], value => textBox.Text = ToString(value));
            }

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            processElementProperty(controlSpec["placeholder"], value => textBox.PlaceholderText = ToString(value));

            textBox.TextChanged += textBox_TextChanged;
        }

        async void textBox_TextChanged(object sender, TextChangedEventArgs e)
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
            if (textBox.FocusState != FocusState.Unfocused)
            {
                updateValueBindingForAttribute("value");
                if (_updateOnChange)
                {
                    await this.StateManager.sendUpdateRequestAsync();
                }
            }
        }
    }
}
