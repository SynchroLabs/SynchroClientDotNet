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
    class WinPasswordBoxWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinPasswordBoxWrapper");

        public WinPasswordBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating password box element with value of: {0}", controlSpec["value"]);
            PasswordBox passwordBox = new PasswordBox();
            this._control = passwordBox;

            applyFrameworkElementDefaults(passwordBox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return passwordBox.Password; }, value => passwordBox.Password = ToString(value)))
            {
                processElementProperty(controlSpec["value"], value => passwordBox.Password = ToString(value));
            }

            processElementProperty(controlSpec["placeholder"], value => passwordBox.PlaceholderText = ToString(value));

            passwordBox.PasswordChanged += passwordBox_PasswordChanged;
        }

        void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;

            // Edit controls have a bad habit of posting a text changed event, and there are cases where 
            // this event is generated based on programmatic setting of text and comes in asynchronously
            // after that programmatic action, making it difficult to distinguish actual user changes.
            // This shortcut will help a lot of the time, but there are still cases where this will be 
            // signalled incorrectly (such as in the case where a control with focus is the target of
            // an update from the server), so we'll do some downstream delta checking as well, but this
            // check will cut down most of the chatter.
            //
            if (passwordBox.FocusState != FocusState.Unfocused)
            {
                updateValueBindingForAttribute("value");
            }
        }
    }
}
