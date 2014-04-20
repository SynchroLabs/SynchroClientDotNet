﻿using MaaasClientWin.Controls;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
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
