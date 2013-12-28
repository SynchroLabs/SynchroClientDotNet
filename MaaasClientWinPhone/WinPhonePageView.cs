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

        public WinPhonePageView(StateManager stateManager, ViewModel viewModel, Panel panel) :
            base(stateManager, viewModel)
        {
            _rootControlWrapper = new WinPhoneControlWrapper(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return WinPhoneControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
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
                panel.Children.Add(((WinPhoneControlWrapper)content).Control);
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
