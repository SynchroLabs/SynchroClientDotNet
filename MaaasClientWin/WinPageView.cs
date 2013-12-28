using MaaasClientWin.Controls;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin
{
    class WinPageView : PageView
    {
        WinControlWrapper _rootControlWrapper;

        public WinPageView(StateManager stateManager, ViewModel viewModel, Panel panel) :
            base(stateManager, viewModel)
        {
            _rootControlWrapper = new WinControlWrapper(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return WinControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public override void ClearContent()
        {
            Panel panel = (Panel)_rootControlWrapper.Control;
            panel.Children.Clear();
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            Panel panel = (Panel)_rootControlWrapper.Control;
            if (content != null)
            {
                panel.Children.Add(((WinControlWrapper)content).Control);
            }
            _rootControlWrapper.ChildControls.Add(content);
        }

        //
        // MessageBox stuff...
        //

        private void MessageDialogCommandHandler(IUICommand command)
        {
            Util.debug("MessageBox Command invoked: " + command.Label);
            if (command.Id != null)
            {
                Util.debug("MessageBox command: " + (string)command.Id);
                _stateManager.processCommand((string)command.Id);
            }
        }

        public override async void ProcessMessageBox(JObject messageBox)
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
    }
}
