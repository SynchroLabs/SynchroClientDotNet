using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Threading.Tasks;

namespace MaaasClientIOS.Controls
{
    class iOSToolBarToggleWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSToolBarToggleWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle.Attribute };

        protected UIBarButtonItem _buttonItem;

        protected bool _isChecked = false;
        protected string _uncheckedText;
        protected string _checkedText;
        protected string _uncheckedIcon;
        protected string _checkedIcon;

        protected void setText(string value)
        {
            if (value != null)
            {
                _buttonItem.Title = value;
            }
        }

        protected void setImage(string value)
        {
            if (value != null)
            {
                _buttonItem.Image = iOSToolBarWrapper.LoadIconImage(value);
                this._pageView.SetNavBarButton(_buttonItem);
            }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    if (_isChecked)
                    {
                        _buttonItem.Title = _checkedText;
                        setImage(_checkedIcon);
                    }
                    else
                    {
                        _buttonItem.Title = _uncheckedText;
                        setImage(_uncheckedIcon);
                    }
                }
            }
        }

        public string UncheckedText
        {
            get { return _uncheckedText; }
            set
            {
                _uncheckedText = value;
                if (!_isChecked)
                {
                    setText(_uncheckedText);
                }
            }
        }

        public string CheckedText
        {
            get { return _checkedText; }
            set
            {
                _checkedText = value;
                if (_isChecked)
                {
                    setText(_checkedText);
                }
            }
        }

        public string UncheckedIcon
        {
            get { return _uncheckedIcon; }
            set
            {
                _uncheckedIcon = value;
                if (!_isChecked)
                {
                    setImage(_uncheckedIcon);
                }
            }
        }

        public string CheckedIcon
        {
            get { return _checkedIcon; }
            set
            {
                _checkedIcon = value;
                if (_isChecked)
                {
                    setImage(_checkedIcon);
                }
            }
        }

        public iOSToolBarToggleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating tool bar toggle button element");

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            // Custom items, can specify text, icon, or both
            //
            _buttonItem = new UIBarButtonItem("", UIBarButtonItemStyle.Plain, buttonItem_Clicked);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new JValue(this.IsChecked); }, value => this.IsChecked = ToBoolean(value)))
            {
                processElementProperty(controlSpec["value"], value => this.IsChecked = ToBoolean(value));
            }

            processElementProperty(controlSpec["text"], value => _buttonItem.Title = ToString(value));
            processElementProperty(controlSpec["icon"], value => _buttonItem.Image = UIImage.FromBundle("icons/blue/" + ToString(value)));

            processElementProperty(controlSpec["uncheckedtext"], value => this.UncheckedText = ToString(value));
            processElementProperty(controlSpec["checkedtext"], value => this.CheckedText = ToString(value));
            processElementProperty(controlSpec["uncheckedicon"], value => this.UncheckedIcon = ToString(value));
            processElementProperty(controlSpec["checkedicon"], value => this.CheckedIcon = ToString(value));

            processElementProperty(controlSpec["enabled"], value => _buttonItem.Enabled = ToBoolean(value));

            if ((string)controlSpec["control"] == "navBar.toggle")
            {
                // When image and text specified, uses image.  Image is placed on button surface verbatim (no color coersion).
                //
                _pageView.SetNavBarButton(_buttonItem);
            }
            else // toolBar.toggle
            {
                // Can use image, text, or both, and toolbar shows what was provided (including image+text).  Toolbar coerces colors
                // and handles disabled state (for example, on iOS 6, icons/text show up as white when enabled and gray when disabled).
                //
                _pageView.AddToolbarButton(_buttonItem);
            }

            _isVisualElement = false;
        }

        async void buttonItem_Clicked(object sender, EventArgs e)
        {
            this.IsChecked = !this.IsChecked;

            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnToggle);
            if (command != null)
            {
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}