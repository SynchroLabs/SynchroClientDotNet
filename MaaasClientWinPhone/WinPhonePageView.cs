using MaaasClientWinPhone.Controls;
using MaaasCore;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.GamerServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

// Transitions
//
//     http://codingdroid.com/navigation-with-transition-effects-wp8-app-development-tutorial-6/

namespace MaaasClientWinPhone
{
    public class WinPhonePageView : PageView
    {
        MainPage _page;
        WinPhoneControlWrapper _rootControlWrapper;

        public WinPhonePageView(StateManager stateManager, ViewModel viewModel, MainPage page, ContentControl contentControl) :
            base(stateManager, viewModel)
        {
            _page = page;
            _rootControlWrapper = new WinPhoneControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, contentControl);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return WinPhoneControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        protected IApplicationBar ShowAppBar()
        {
            if (_page.ApplicationBar == null)
            {
                _page.ApplicationBar = new ApplicationBar();

                _page.ApplicationBar.Mode = ApplicationBarMode.Default;
                _page.ApplicationBar.Opacity = 1.0;
                _page.ApplicationBar.IsVisible = true;
                _page.ApplicationBar.IsMenuEnabled = true;
            }

            return _page.ApplicationBar;
        }

        protected void DestroyAppBar()
        {
            _page.ApplicationBar = null;
        }

        public void AddAppBarIconButton(ApplicationBarIconButton button)
        {
            IApplicationBar appBar = this.ShowAppBar();
            appBar.Buttons.Add(button);
        }

        public void AddAppBarMenuItem(ApplicationBarMenuItem menuItem)
        {
            IApplicationBar appBar = this.ShowAppBar();
            appBar.MenuItems.Add(menuItem);
        }

        public override void ClearContent()
        {
            DestroyAppBar();
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

            string title = " "; // Can't be empty my ass.
            if (messageBox["title"] != null)
            {
                title = PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext);
            }

            List<string> buttonLabels = new List<string>();
            List<string> buttonCommands = new List<string>();

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    buttonLabels.Add(label);
                    buttonCommands.Add(command);
                }
            }
            else
            {
                string label = "Close";
                string command = null;
                buttonLabels.Add(label);
                buttonCommands.Add(command);
            }

            IAsyncResult result = Guide.BeginShowMessageBox(
                 title,
                 message,
                 buttonLabels.ToArray(),
                 0,
                 Microsoft.Xna.Framework.GamerServices.MessageBoxIcon.None,
                 null,
                 null);

            result.AsyncWaitHandle.WaitOne();

            int? choice = Guide.EndShowMessageBox(result);
            if (choice.HasValue)
            {
                string command = buttonCommands[(int)choice];
                Util.debug("User chose button #" + choice);
                if (command != null)
                {
                    Util.debug("MessageBox command: " + command);
                    _stateManager.processCommand(command);
                }
            }
        }
    }
}
