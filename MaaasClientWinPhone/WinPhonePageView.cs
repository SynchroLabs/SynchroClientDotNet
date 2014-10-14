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
using System.Windows;
using System.Windows.Controls;

// Transitions
//
//     http://codingdroid.com/navigation-with-transition-effects-wp8-app-development-tutorial-6/

namespace MaaasClientWinPhone
{
    public class WinPhonePageView : PageView
    {
        static Logger logger = Logger.GetLogger("WinPhonePageView");

        MaaasPage _page;
        WinPhoneControlWrapper _rootControlWrapper;

        public WinPhonePageView(StateManager stateManager, ViewModel viewModel, MaaasPage page, ContentControl contentControl, Action doBackToMenu = null) :
            base(stateManager, viewModel, doBackToMenu)
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

            ScrollViewer mainScroll = contentControl as ScrollViewer;
            if (mainScroll != null)
            {
                // Reset the scroll to the top (you have to do it here while the content is still present.  It doesn't
                // work if you do it after removing the content, or after adding the new content (at least not immediately
                // after).
                //
                mainScroll.ScrollToHorizontalOffset(0);
                mainScroll.ScrollToVerticalOffset(0);
            }

            contentControl.Content = null;
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            ScrollViewer mainScroll = contentControl as ScrollViewer;

            WinPhoneControlWrapper controlWrapper = content as WinPhoneControlWrapper;

            if (mainScroll != null)
            {
                if (controlWrapper != null)
                {
                    // Default scroll behavior had the effect of allowing the contained item to grow
                    // unbounded (when using "Stretch" sizing).  So for example, if you had a text item 
                    // that spanned the content area and was sized with "*", once it filled the space it
                    // would continue to expand (growing the scroll content) instead of wrapping to the 
                    // scroll content area.  
                    //
                    // To address this, we disable scrolling in the dimension of any "stretch" sizing, which
                    // will contain the child in that dimension.
                    //
                    if (controlWrapper.Control.HorizontalAlignment == HorizontalAlignment.Stretch)
                    {
                        mainScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    }
                    else
                    {
                        mainScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    }

                    if (controlWrapper.Control.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        mainScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    }
                    else
                    {
                        mainScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    }
                }
            }

            if (content != null)
            {
                contentControl.Content = (((WinPhoneControlWrapper)content).Control);
            }

            _rootControlWrapper.ChildControls.Add(content);
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox, CommandHandler onCommand)
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
                logger.Debug("User chose button #{0}", choice);
                if (command != null)
                {
                    logger.Debug("MessageBox command: {0}", command);
                    onCommand(command);
                }
            }
        }
    }
}
