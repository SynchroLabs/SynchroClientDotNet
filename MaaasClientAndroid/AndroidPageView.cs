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
using MaaasClientAndroid.Controls;
using Newtonsoft.Json.Linq;
using Android.Util;

namespace MaaasClientAndroid
{
    public class AndroidActionBarItem
    {
        protected string _title;
        protected Action _onItemSelected;
        // !!! icon 
        // !!! ShowAsAction

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
            }
        }

        public Action OnItemSelected { get { return _onItemSelected; } set { _onItemSelected = value; } }

        public AndroidActionBarItem()
        {
        }
    }

    public class AndroidPageView  : PageView
    {
        Activity _activity;
        AndroidControlWrapper _rootControlWrapper;

        List<AndroidActionBarItem> _actionBarItems = new List<AndroidActionBarItem>();

        public AndroidPageView(StateManager stateManager, ViewModel viewModel, Activity activity, ViewGroup panel) :
            base(stateManager, viewModel)
        {
            _activity = activity;
            _rootControlWrapper = new AndroidControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, panel);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return AndroidControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public AndroidActionBarItem CreateAndAddActionBarItem()
        {
            AndroidActionBarItem actionBarItem = new AndroidActionBarItem();
            this._actionBarItems.Add(actionBarItem);
            return actionBarItem;
        }

        public bool OnCreateOptionsMenu(IMenu menu)
        {
            Util.debug("Option menu created");
            if (_actionBarItems.Count > 0)
            {
                foreach(var actionBarItem in _actionBarItems)
                {
                    var item = menu.Add(0, 0, 0, actionBarItem.Title);
                    // item.SetIcon(Android.Resource.Drawable.IcDelete);
                    // item.SetShowAsAction(ShowAsAction.IfRoom);
                }
                return true;
            }
            else // No items
            {
                return false;
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            if ((item.ItemId >= 0) && (item.ItemId < _actionBarItems.Count))
            {
                AndroidActionBarItem actionBarItem = _actionBarItems[item.ItemId];
                Util.debug("Action bar item selected - id: " + item.ItemId + ", title: " + actionBarItem.Title);
                if (actionBarItem.OnItemSelected != null)
                {
                    actionBarItem.OnItemSelected();
                }
                return true;
            }

            return false;
        }

        public bool OnCommandBarUp(IMenuItem item)
        {
            Util.debug("Command bar Up button pushed");
            this.OnBackCommand();
            return true;
        }

        public override void ClearContent()
        {
            this._actionBarItems.Clear();
            this._activity.InvalidateOptionsMenu();

            ViewGroup panel = (ViewGroup)_rootControlWrapper.Control;
            panel.RemoveAllViews(); 
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            ViewGroup panel = (ViewGroup)_rootControlWrapper.Control;
            if (content != null)
            {
                panel.AddView(((AndroidControlWrapper)content).Control);
            }
            _rootControlWrapper.ChildControls.Add(content);

            this._activity.ActionBar.SetDisplayHomeAsUpEnabled(this.HasBackCommand);
            this._activity.InvalidateOptionsMenu();
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(_activity);
            AlertDialog dialog = builder.Create();

            dialog.SetMessage(PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext));
            if (messageBox["title"] != null)
            {
                dialog.SetTitle(PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext));
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                if (options.Count > 0)
                {
                    JObject option = (JObject)options[0];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton(label, (s, ev) =>
                    {
                        Util.debug("MessageBox Command invoked: " + label);
                        if (command != null)
                        {
                            Util.debug("MessageBox command: " + command);
                            _stateManager.processCommand(command);
                        }
                    });
                }

                if (options.Count > 1)
                {
                    JObject option = (JObject)options[1];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton2(label, (s, ev) =>
                    {
                        Util.debug("MessageBox Command invoked: " + label);
                        if (command != null)
                        {
                            Util.debug("MessageBox command: " + command);
                            _stateManager.processCommand(command);
                        }
                    });
                }

                if (options.Count > 2)
                {
                    JObject option = (JObject)options[2];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton3(label, (s, ev) =>
                    {
                        Util.debug("MessageBox Command invoked: " + label);
                        if (command != null)
                        {
                            Util.debug("MessageBox command: " + command);
                            _stateManager.processCommand(command);
                        }
                    });
                }
            }
            else
            {
                // Not commands - add default "close"
                //
                dialog.SetButton("Close", (s, ev) =>
                {
                    Util.debug("MessageBox default close button clicked");
                });
            }

            dialog.Show();
        }
    }
}