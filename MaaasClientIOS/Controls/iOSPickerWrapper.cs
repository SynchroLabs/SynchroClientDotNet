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
    public class PickerModel : UIPickerViewModel
    {
        public IList<Object> values;

        public event EventHandler<PickerChangedEventArgs> PickerChanged;

        public PickerModel(IList<Object> values)
        {
            this.values = values;
        }

        public override int GetComponentCount(UIPickerView picker)
        {
            return 1;
        }

        public override int GetRowsInComponent(UIPickerView picker, int component)
        {
            return values.Count;
        }

        public override string GetTitle(UIPickerView picker, int row, int component)
        {
            return values[row].ToString();
        }

        public override float GetRowHeight(UIPickerView picker, int component)
        {
            return 40f;
        }

        public override void Selected(UIPickerView picker, int row, int component)
        {
            if (this.PickerChanged != null)
            {
                this.PickerChanged(this, new PickerChangedEventArgs { SelectedValue = values[row] });
            }
        }
    }

    public class PickerChangedEventArgs : EventArgs
    {
        public object SelectedValue { get; set; }
    }

    class iOSPickerWrapper : iOSControlWrapper
    {
        // On phones, we have a picker "input view" at the bottom of the screen when "editing", similar to the way the keyboard 
        // pops up there for a regular text field.  This is modelled after:
        //
        //     http://www.gooorack.com/2013/07/18/xamarin-uipickerview-as-a-combobox/
        //
        // On tablets, it might be more appropriate to use a popover near the control to show the list, such as this:
        //
        //     https://github.com/xamarin/monotouch-samples/blob/master/MonoCatalog-MonoDevelop/PickerViewController.xib.cs
        //
        public iOSPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating picker element");

            UITextField textBox = new UITextField();
            textBox.BorderStyle = UITextBorderStyle.RoundedRect;

            this._control = textBox;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(textBox);

            List<Object> state_list = new List<Object>();
            state_list.Add("ACT");
            state_list.Add("NSW");
            state_list.Add("NT");
            state_list.Add("QLD");
            state_list.Add("SA");
            state_list.Add("TAS");
            state_list.Add("VIC");
            state_list.Add("WA");
            PickerModel picker_model = new PickerModel(state_list);

            UIPickerView picker = new UIPickerView();

            picker.Model = picker_model;
            picker.ShowSelectionIndicator = true;

            UIToolbar toolbar = new UIToolbar();
            toolbar.BarStyle = UIBarStyle.Black;
            toolbar.Translucent = true;
            toolbar.SizeToFit();

            UIBarButtonItem doneButton = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, (s, e) =>
            {
                if (textBox.IsFirstResponder)
                {
                    textBox.Text = picker_model.values[picker.SelectedRowInComponent(0)].ToString();
                    textBox.ResignFirstResponder();
                }
            });
            toolbar.SetItems(new UIBarButtonItem[] { doneButton }, true);

            textBox.InputView = picker;
            textBox.InputAccessoryView = toolbar;

            // When the textbox is tapped (bringing up the picker), update the picker selection to match the textbox.
            //
            textBox.TouchDown += (sender, e) =>
            {
                // !!! Since the textbox is always set from the picker, this ends up being a no-op.  But if
                //     the textbox value had some external way of being updated (data binding), then we'd need
                //     to update the picker selection at the time of such update (or when clicked, as here).
                //
                picker.Select(picker_model.values.IndexOf(textBox.Text), 0, true);
            };
        }
    }
}