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
using System.Threading.Tasks;

namespace SynchroClientAndroid.Controls
{
    class AndroidActionToggleWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidActionToggleWrapper");

        static string[] Commands = new string[] { CommandName.OnToggle.Attribute };

        protected AndroidActionBarItem _actionBarItem;

        protected bool _isChecked = false;
        protected string _uncheckedText;
        protected string _checkedText;
        protected string _uncheckedIcon;
        protected string _checkedIcon;

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
                        if (_checkedText != null)
                        {
                            _actionBarItem.Title = _checkedText;
                        }
                        if (_checkedIcon != null)
                        {
                            _actionBarItem.Icon = _checkedIcon;
                        }
                    }
                    else
                    {
                        if (_uncheckedText != null)
                        {
                            _actionBarItem.Title = _uncheckedText;
                        }
                        if (_uncheckedIcon != null)
                        {
                            _actionBarItem.Icon = _uncheckedIcon;
                        }
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
                    _actionBarItem.Title = _uncheckedText;
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
                    _actionBarItem.Title = _checkedText;
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
                    _actionBarItem.Icon = _uncheckedIcon;
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
                    _actionBarItem.Icon = _checkedIcon;
                }
            }
        }

        public AndroidActionToggleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating action bar toggle item with title of: {0}", controlSpec["text"]);

            this._isVisualElement = false;

            _actionBarItem = _pageView.CreateAndAddActionBarItem();

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (!processElementBoundValue("value", (string)bindingSpec["value"], () => { return new MaaasCore.JValue(this.IsChecked); }, value => this.IsChecked = ToBoolean(value)))
            {
                processElementProperty(controlSpec["value"], value => this.IsChecked = ToBoolean(value));
            }

            processElementProperty(controlSpec["text"], value => _actionBarItem.Title = ToString(value));
            processElementProperty(controlSpec["icon"], value => _actionBarItem.Icon = ToString(value));

            processElementProperty(controlSpec["uncheckedtext"], value => this.UncheckedText = ToString(value));
            processElementProperty(controlSpec["checkedtext"], value => this.CheckedText = ToString(value));
            processElementProperty(controlSpec["uncheckedicon"], value => this.UncheckedIcon = ToString(value));
            processElementProperty(controlSpec["checkedicon"], value => this.CheckedIcon = ToString(value));

            processElementProperty(controlSpec["enabled"], value => _actionBarItem.IsEnabled = ToBoolean(value));

            _actionBarItem.ShowAsAction = ShowAsAction.Never;
            if (controlSpec["showAsAction"] != null)
            {
                if ((string)controlSpec["showAsAction"] == "Always")
                {
                    _actionBarItem.ShowAsAction = ShowAsAction.Always;
                }
                else if ((string)controlSpec["showAsAction"] == "IfRoom")
                {
                    _actionBarItem.ShowAsAction = ShowAsAction.IfRoom;
                }
            }

            if (controlSpec["showActionAsText"] != null)
            {
                if (ToBoolean(controlSpec["showActionAsText"]))
                {
                    _actionBarItem.ShowAsAction |= ShowAsAction.WithText;
                }
            }

            _actionBarItem.OnItemSelected = this.onItemSelected;
        }

        public async void onItemSelected()
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
