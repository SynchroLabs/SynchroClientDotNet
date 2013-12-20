using MaaasCore;
using MaaasClientWinPhone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Windows.Controls;
using System.Windows;

namespace MaaasClientWinPhone
{
    class PageView
    {
        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; }
        public Panel Content { get; set; }

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
            Panel panel = this.Content;
            panel.Children.Clear();

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

            WinPhoneControlWrapper controlWrapper = WinPhoneControlWrapper.WrapControl(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);
            controlWrapper.createControls((JArray)pageView["elements"], (childControlSpec, childControlWrapper) =>
            {
                panel.Children.Add(childControlWrapper.Control);
            });
        }

        //
        // MessageBox stuff...
        //

        public void processMessageBox(JObject messageBox)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
        }

        /* !!!
        private void MessageDialogCommandHandler(IUICommand command)
        {
            Util.debug("MessageBox Command invoked: " + command.Label);
            if (command.Id != null)
            {
                Util.debug("MessageBox command: " + (string)command.Id);
                _stateManager.processCommand((string)command.Id);
            }
        }

        public async void processMessageBox(JObject messageBox)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);

            var messageDialog = new MessageDialog(message);

            if (messageBox["title"] != null)
            {
                messageDialog.Title = PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext);
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    if ((string)option["command"] != null)
                    {
                        messageDialog.Commands.Add(new UICommand(
                            PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler),
                            PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext))
                            );
                    }
                    else
                    {
                        messageDialog.Commands.Add(new UICommand(
                            PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler))
                            );
                    }
                }
            }

            await messageDialog.ShowAsync();
        }
        */
    }
}

