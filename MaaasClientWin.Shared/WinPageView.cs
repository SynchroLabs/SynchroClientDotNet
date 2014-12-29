using MaaasClientWin.Controls;
using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin
{
    class WinPageView : PageView
    {
        static Logger logger = Logger.GetLogger("WinPageView");

        Page _page;
        WinControlWrapper _rootControlWrapper;

        public Page Page { get { return _page; } }

        public WinPageView(StateManager stateManager, ViewModel viewModel, Page page, ContentControl contentControl, Action doBackToMenu = null) :
            base(stateManager, viewModel, doBackToMenu)
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

            ScrollViewer mainScroll = contentControl as ScrollViewer;
            if (mainScroll != null)
            {
                // Reset the scroll to the top (you have to do it here while the content is still present.  It doesn't
                // work if you do it after removing the content, or after adding the new content (at least not immediately
                // after).
                //
                mainScroll.ChangeView(0, 0, 1.0f, true);
            }

            contentControl.Content = null;
            _rootControlWrapper.ChildControls.Clear();
            ClearAppBars();
        }

        public override void SetContent(ControlWrapper content)
        {
            ContentControl contentControl = (ContentControl)_rootControlWrapper.Control;
            ScrollViewer mainScroll = contentControl as ScrollViewer;

            WinControlWrapper controlWrapper = content as WinControlWrapper;

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
                        mainScroll.HorizontalScrollMode = ScrollMode.Disabled;
                    }
                    else
                    {
                        mainScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        mainScroll.HorizontalScrollMode = ScrollMode.Enabled;
                    }

                    if (controlWrapper.Control.VerticalAlignment == VerticalAlignment.Stretch)
                    {
                        mainScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        mainScroll.VerticalScrollMode = ScrollMode.Disabled;
                    }
                    else
                    {
                        mainScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                        mainScroll.VerticalScrollMode = ScrollMode.Enabled;
                    }
                }
            }

            if (content != null)
            {
                contentControl.Content = ((WinControlWrapper)content).Control;
            }
            _rootControlWrapper.ChildControls.Add(content);
        }

        public void ClearAppBars()
        {
#if WINDOWS_PHONE_APP
            if (_page.BottomAppBar != null)
            {
                // This is not ideal, but seems to work.  In Win you can just set the appBar to null and it does
                // the right thing.  In WinPhone, that causes a freakout.  Se we clear the commands, which you'd 
                // think would be enough, but still shows an empty command bar, so then we have to manually hide it.
                //
                CommandBar commandBar = (CommandBar)_page.BottomAppBar;
                commandBar.PrimaryCommands.Clear();
                commandBar.SecondaryCommands.Clear();
                commandBar.Visibility = Visibility.Collapsed;
            }
#else
            _page.TopAppBar = null;
            _page.BottomAppBar = null;
#endif
        }

        //
        // MessageBox stuff...
        //

        public override async void ProcessMessageBox(JObject messageBox, CommandHandler onCommand)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);

            var messageDialog = new MessageDialog(message);

            if (messageBox["title"] != null)
            {
                messageDialog.Title = PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext);
            }

            UICommandInvokedHandler handler = new UICommandInvokedHandler(delegate(IUICommand command)
            {
                logger.Debug("MessageBox Command invoked: {0}", command.Label);
                if (command.Id != null)
                {
                    logger.Debug("MessageBox command: {0}", (string)command.Id);
                    onCommand((string)command.Id);
                }
            });

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    if ((string)option["command"] != null)
                    {
                        messageDialog.Commands.Add(
                            new UICommand(
                                PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext),
                                handler,
                                PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext)
                            )
                        );
                    }
                    else
                    {
                        messageDialog.Commands.Add(
                            new UICommand(
                                PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext),
                                handler
                            )
                        );
                    }
                }
            }

            await messageDialog.ShowAsync();
        }
    }
}
