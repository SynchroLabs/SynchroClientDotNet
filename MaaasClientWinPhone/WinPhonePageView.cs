using MaaasClientWinPhone.Controls;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone
{
    class WinPhonePageView : PageView
    {
        WinPhoneControlWrapper _rootControlWrapper;

        public WinPhonePageView(StateManager stateManager, ViewModel viewModel, ContentControl contentControl) :
            base(stateManager, viewModel)
        {
            _rootControlWrapper = new WinPhoneControlWrapper(_stateManager, _viewModel, _viewModel.RootBindingContext, contentControl);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return WinPhoneControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public override void ClearContent()
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            contentControl.Content = null;
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            if (content != null)
            {
                contentControl.Content = (((WinPhoneControlWrapper)content).Control);
            }
            _rootControlWrapper.ChildControls.Add(content);
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
        }
    }
}
