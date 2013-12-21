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

        string onBackCommand = null;

        public PageView(StateManager stateManager, ViewModel viewModel)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
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
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
        }
    }
}

