using SynchroCore;
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

        bool   _updateOnChange = false;
        bool   _multiline = false;
        double _lines = 0;

        public WinTextBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating text box element with value of: {0}", controlSpec["value"]);
            TextBox textBox = new TextBox();
            this._control = textBox;

            if ((controlSpec["multiline"] != null) && (bool)controlSpec["multiline"])
            {
                // Mutliline...
                _multiline = true;
                textBox.TextWrapping = TextWrapping.Wrap;
                textBox.AcceptsReturn = true;

                processElementProperty(controlSpec, "lines", value => 
                {
                    _lines = ToDouble(value);
                    this.OnFontChange(_control); 
                });
            }

            applyFrameworkElementDefaults(textBox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new JValue(textBox.Text); }, value => textBox.Text = ToString(value)))
            {
                processElementProperty(controlSpec, "value", value => textBox.Text = ToString(value));
            }

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            processElementProperty(controlSpec, "placeholder", value => textBox.PlaceholderText = ToString(value));

            textBox.TextChanged += textBox_TextChanged;
        }

        public override void OnFontChange(FrameworkElement control)
        {
            if (!_heightSpecified && (_lines >= 1))
            {
                var textBox = (TextBox)control;

                // !!! The reported FontSize doesn't return the baseline-to-baseline font size.  There is also no way
                //     to get any font metrics (from the control or the system at large) that would help determine
                //     how tall you have to make the TextBox to accomodated a certain numbers of lines of text in
                //     a given font.  After a lot of research, I gave up and came up with this value through trial and
                //     error that works for number of lines 1-5 with the standard system font.  Fuck you Microsoft.
                //
                var fudge = 1.4; 
                textBox.Height = (textBox.FontSize * _lines * fudge) + textBox.Padding.Top + textBox.Padding.Bottom;
            }
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
