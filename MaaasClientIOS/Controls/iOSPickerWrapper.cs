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
    public class BindingContextPickerModel : UIPickerViewModel
    {
        protected List<BindingContext> _bindingContexts;
        protected string _itemContent;

        public BindingContextPickerModel() : base()
        {
        }

        public void SetContents(BindingContext bindingContext, string itemContent)
        {
            _bindingContexts = bindingContext.SelectEach("$data");
            _itemContent = itemContent;
        }

        public override int GetComponentCount(UIPickerView picker)
        {
            return 1;
        }

        public override int GetRowsInComponent(UIPickerView picker, int component)
        {
            return (_bindingContexts != null) ? _bindingContexts.Count : 0;
        }

        public override string GetTitle(UIPickerView picker, int row, int component)
        {
            return PropertyValue.ExpandAsString(_itemContent, _bindingContexts[row]);
        }

        public JToken GetValue(int row)
        {
            return _bindingContexts[row].Select("$data").GetValue();
        }

        public JToken GetSelection(int row, string selectionItem)
        {
            return _bindingContexts[row].Select(selectionItem).GetValue().DeepClone();
        }

        public BindingContext GetBindingContext(int row)
        {
            return _bindingContexts[row];
        }

        public override float GetRowHeight(UIPickerView picker, int component)
        {
            return 40f;
        }

        public override void Selected(UIPickerView picker, int row, int component)
        {
            // This fires whenever an item is selected in the picker view (meaning that the item has been
            // scrolled to and highlighted, not that the user as necessarily "chosen" the value in the sense
            // that we are interested in here).  So for that reason, this isn't really useful.  We instead
            // watch the "Done" button and grab the selection when the picker is dismissed.
        }
    }

    public class PickerTextField : UITextField
    {
        // This gets rid of the blinking caret when "editing" (which in our case, means having the picker input view up).
        //
        public override RectangleF GetCaretRectForPosition(UITextPosition position)
        {
            return RectangleF.Empty;
        }
    }

    class iOSPickerWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSPickerWrapper");

        // On phones, we have a picker "input view" at the bottom of the screen when "editing", similar to the way the keyboard 
        // pops up there for a regular text field.  This is modelled after:
        //
        //     http://www.gooorack.com/2013/07/18/xamarin-uipickerview-as-a-combobox/
        //
        // On tablets, it might be more appropriate to use a popover near the control to show the list, such as this:
        //
        //     https://github.com/xamarin/monotouch-samples/blob/master/MonoCatalog-MonoDevelop/PickerViewController.xib.cs
        //

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        int _lastSelectedPosition = -1;

        PickerTextField _textBox;

        static string[] Commands = new string[] { CommandName.OnSelectionChange };

        public iOSPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating picker element");

            _textBox = new PickerTextField();
            _textBox.BorderStyle = UITextBorderStyle.RoundedRect;

            this._control = _textBox;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(_textBox);

            UIPickerView picker = new UIPickerView();

            picker.Model = new BindingContextPickerModel();
            picker.ShowSelectionIndicator = true;

            UIToolbar toolbar = new UIToolbar();
            toolbar.BarStyle = UIBarStyle.Black;
            toolbar.Translucent = true;
            toolbar.SizeToFit();

            UIBarButtonItem doneButton = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, (s, e) =>
            {
                if (_textBox.IsFirstResponder)
                {
                    int row = picker.SelectedRowInComponent(0);
                    _textBox.Text = picker.Model.GetTitle(picker, row, 0);
                    _textBox.ResignFirstResponder();
                    this.picker_ItemSelected(picker, row);
                }
            });
            toolbar.SetItems(new UIBarButtonItem[] { doneButton }, true);

            _textBox.InputView = picker;
            _textBox.InputAccessoryView = toolbar;

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getPickerContents(picker),
                    value => this.setPickerContents(picker, GetValueBinding("items").BindingContext, itemContent));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getPickerSelection(picker, selectionItem),
                    value => this.setPickerSelection(picker, selectionItem, (JToken)value));
            }
        }

        public JToken getPickerContents(UIPickerView picker)
        {
            logger.Debug("Getting picker contents - NOOP");
            throw new NotImplementedException();
        }

        public void setPickerContents(UIPickerView picker, BindingContext bindingContext, string itemContent)
        {
            logger.Debug("Setting picker contents");

            _selectionChangingProgramatically = true;

            BindingContextPickerModel model = (BindingContextPickerModel)picker.Model;
            model.SetContents(bindingContext, itemContent);

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                selectionBinding.UpdateViewFromViewModel();
            }
            else if (_localSelection != null)
            {
                // If there is not a "selection" value binding, then we use local selection state to restore the selection when
                // re-filling the list.
                //
                this.setPickerSelection(picker, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }
        public JToken getPickerSelection(UIPickerView picker, string selectionItem)
        {
            BindingContextPickerModel model = (BindingContextPickerModel)picker.Model;

            if (picker.SelectedRowInComponent(0) >= 0)
            {
                return model.GetSelection(picker.SelectedRowInComponent(0), selectionItem);
            }
            return new Newtonsoft.Json.Linq.JValue(false); // This is a "null" selection
        }

        public void setPickerSelection(UIPickerView picker, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            BindingContextPickerModel model = (BindingContextPickerModel)picker.Model;

            for (int i = 0; i < model.GetRowsInComponent(picker, 0); i++)
            {
                if (JToken.DeepEquals(selection, model.GetSelection(i, selectionItem)))
                {
                    int row = picker.SelectedRowInComponent(0);
                    _textBox.Text = picker.Model.GetTitle(picker, i, 0);
                    _lastSelectedPosition = i;
                    picker.Select(i, 0, true);
                    break;
                }
            }

            _selectionChangingProgramatically = false;
        }

        void picker_ItemSelected(UIPickerView picker, int row)
        {
            logger.Debug("Picker selection changed");

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getPickerSelection(picker, "$data");
            }

            if ((!_selectionChangingProgramatically) && (row != _lastSelectedPosition))
            {
                _lastSelectedPosition = row;
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    logger.Debug("Picker item click with command: {0}", command);

                    // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                    //
                    BindingContextPickerModel model = (BindingContextPickerModel)picker.Model;
                    Task t = StateManager.processCommand(command.Command, command.GetResolvedParameters(model.GetBindingContext(row)));
                }
            }
        }
    }
}