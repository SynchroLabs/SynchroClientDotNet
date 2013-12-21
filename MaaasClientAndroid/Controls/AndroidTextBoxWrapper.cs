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
using Newtonsoft.Json.Linq;
using Android.Text;

namespace MaaasClientAndroid.Controls
{
    class AndroidTextBoxWrapper : AndroidControlWrapper
    {
        public AndroidTextBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating text box element with caption of: " + controlSpec["caption"]);

            EditText editText = new EditText(((AndroidControlWrapper)parent).Control.Context);
            this._control = editText;

            applyFrameworkElementDefaults(editText);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return editText.Text; }, value => editText.Text = ToString(value)))
            {
                processElementProperty((string)controlSpec["value"], value => editText.Text = ToString(value));
            }
            editText.TextChanged += editText_TextChanged;
        }

        void editText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var editText = sender as EditText;

            // Edit controls have a bad habit of posting a text changed event, and there are cases where 
            // this event is generated based on programmatic setting of text and comes in asynchronously
            // after that programmatic action, making it difficult to distinguish actual user changes.
            // This shortcut will help a lot of the time, but there are still cases where this will be 
            // signalled incorrectly (such as in the case where a control with focus is the target of
            // an update from the server), so we'll do some downstream delta checking as well, but this
            // check will cut down most of the chatter.
            //
            if (editText.IsFocused)
            {
                updateValueBindingForAttribute("value");
            }
        }
    }
}