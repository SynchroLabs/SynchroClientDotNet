using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace MaaasClientIOS.Controls
{
    class iOSPickerWrapper : iOSControlWrapper
    {
        // On phones, we will have a picker view at the bottom of the screen, similar to the way the keyboard pops up there. 
        // On tablets, it might be more appropriate to use a popover near the control to show the list.
        //
        public iOSPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");

            UITextField textBox = new UITextField();
            textBox.BorderStyle = UITextBorderStyle.RoundedRect;

            UIPickerView picker = new UIPickerView();
            this._control = picker;

            applyFrameworkElementDefaults(picker);

            // Xamarin example...
            //
            // http://www.gooorack.com/2013/07/18/xamarin-uipickerview-as-a-combobox/

            // !!! UIActionSheet to hold picker?
            //
            // http://www.wetware.co.nz/2009/02/how-to-popup-a-uipickerview-from-the-bottom-of-a-uiscrollview-in-response-to-uitextfield-selection/
            //
            // https://github.com/xamarin/monotouch-samples/blob/master/MonoCatalog-MonoDevelop/PickerViewController.xib.cs
        }
    }
}