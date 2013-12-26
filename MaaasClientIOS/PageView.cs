using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using MaaasClientIOS.Controls;
using System.Drawing;

namespace MaaasClientIOS
{
    class MaaasNavigationBarDelegate : UINavigationBarDelegate
    {
        PageView _pageView;

        public MaaasNavigationBarDelegate(PageView pageView)
        {
            _pageView = pageView;
        }

        public override bool ShouldPopItem(UINavigationBar navigationBar, UINavigationItem item)
        {
            Util.debug("Should pop item got called!");
            _pageView.OnBackCommand();
            return false;
        }
    }

    class PageView
    {
        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; }
        public UIView Content { get; set; }

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
            UIView panel = this.Content;

            // Clear out the panel
            foreach (var subview in panel.Subviews)
            {
                subview.RemoveFromSuperview();
            }

            this.onBackCommand = (string)pageView["onBack"];
            this.setBackEnabled(this.onBackCommand != null);

            string pageTitle = (string)pageView["title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            float padding = 10;
            float currentTop = padding;
            float currentLeft = padding;

            UINavigationBar navBar = new UINavigationBar(new System.Drawing.RectangleF(0f, 0f, panel.Frame.Width, 42f));

            if (this.onBackCommand != null)
            {
                // Add a "Back" context and a delegate to handle the back command...
                //
                UINavigationItem navItemBack = new UINavigationItem("Back");
                navBar.PushNavigationItem(navItemBack, false);
                navBar.Delegate = new MaaasNavigationBarDelegate(this);
            }

            UINavigationItem navItem = new UINavigationItem(pageTitle);
            navBar.PushNavigationItem(navItem, false);

            panel.AddSubview(navBar);
            currentTop += navBar.Bounds.Height;

            iOSControlWrapper controlWrapper = iOSControlWrapper.WrapControl(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);
            controlWrapper.createControls((JArray)pageView["elements"], (childControlSpec, childControlWrapper) =>
            {
                // !!! Worlds worst stackpanel (consolidate with stackpanel, if we need to inject a top level default container)
                RectangleF frame = childControlWrapper.Control.Frame;
                frame.X = currentLeft;
                frame.Y = currentTop;
                childControlWrapper.Control.Frame = frame;
                currentTop += childControlWrapper.Control.Bounds.Height + padding;
                panel.AddSubview(childControlWrapper.Control);
            });
        }

        //
        // MessageBox stuff...
        //

        public void processMessageBox(JObject messageBox)
        {
            // !!! Implement MessageBox support
            //
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
            Util.debug("Message box with message: " + message);
        }
    }
}