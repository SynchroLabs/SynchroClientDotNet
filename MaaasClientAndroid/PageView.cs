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
using MaaasClientAndroid.Controls;

namespace MaaasClientAndroid
{
    class PageView
    {
        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; }
        public ViewGroup Content { get; set; }

        StateManager _stateManager;
        ViewModel _viewModel;
        Activity _activity;

        string onBackCommand = null;

        public PageView(StateManager stateManager, ViewModel viewModel, Activity activity)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
            _activity = activity;
        }

        public void OnBackCommand()
        {
            Util.debug("Back button click with command: " + onBackCommand);
            _stateManager.processCommand(onBackCommand);
        }

        public void processPageView(JObject pageView)
        {
            ViewGroup panel = this.Content;
            panel.RemoveAllViews();

            this.onBackCommand = (string)pageView["onBack"];
            if (this.setBackEnabled != null)
            {
                this.setBackEnabled(this.onBackCommand != null);
            }

            string pageTitle = (string)pageView["title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            AndroidControlWrapper controlWrapper = AndroidControlWrapper.WrapControl(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);
            controlWrapper.createControls((JArray)pageView["elements"], (childControlSpec, childControlWrapper) =>
            {
                panel.AddView(childControlWrapper.Control);
            });
        }

        //
        // MessageBox stuff...
        //

        public void processMessageBox(JObject messageBox)
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

