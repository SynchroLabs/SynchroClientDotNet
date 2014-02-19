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
        Page _page;
        WinControlWrapper _rootControlWrapper;

        public Page Page { get { return _page; } }

        public WinPageView(StateManager stateManager, ViewModel viewModel, Page page, ContentControl contentControl) :
            base(stateManager, viewModel)
        {
            _page = page;
            _rootControlWrapper = new WinControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, contentControl);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return WinControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public override void ClearContent()
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            contentControl.Content = null;
            _rootControlWrapper.ChildControls.Clear();
            ClearAppBars();
        }

        public override void SetContent(ControlWrapper content)
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            if (content != null)
            {
                contentControl.Content = ((WinControlWrapper)content).Control;
            }
            _rootControlWrapper.ChildControls.Add(content);
        }

        public void ClearAppBars()
        {
            _page.TopAppBar = null;
            _page.BottomAppBar = null;
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
