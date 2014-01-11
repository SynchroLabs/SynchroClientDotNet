using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using MaaasClientIOS.Controls;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS
{
    class MaaasNavigationBarDelegate : UINavigationBarDelegate
    {
        iOSPageView _pageView;

        public MaaasNavigationBarDelegate(iOSPageView pageView)
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

    class iOSPageView : PageView
    {
        iOSControlWrapper _rootControlWrapper;
        string _pageTitle = "";

        public iOSPageView(StateManager stateManager, ViewModel viewModel, UIView panel) :
            base(stateManager, viewModel)
        {
            _rootControlWrapper = new iOSControlWrapper(_stateManager, _viewModel, _viewModel.RootBindingContext, panel);

            this.setPageTitle = title => _pageTitle = title;

            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.DidShowNotification, notification => onKeyboardShown(notification));
            NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, notification => onKeyboardHidden(notification));

            DismissKeyboardOnBackgroundTap();
        }

        protected void DismissKeyboardOnBackgroundTap()
        {
            // Add gesture recognizer to hide keyboard
            var tap = new UITapGestureRecognizer { CancelsTouchesInView = false };
            tap.AddTarget(() => _rootControlWrapper.Control.EndEditing(true));
            tap.ShouldReceiveTouch += (recognizer, touch) => !(touch.View is UIButton);
            _rootControlWrapper.Control.AddGestureRecognizer(tap);
        }

        public void onKeyboardShown(NSNotification notification)
        {
            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
            Util.debug("Keyboard shown - frame: " + keyboardFrame);
        }

        public void onKeyboardHidden(NSNotification notification)
        {
            Util.debug("Keyboard hidden");
            var keyboardFrame = UIKeyboard.FrameBeginFromNotification(notification);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return iOSControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public override void ClearContent()
        {
            UIView panel = _rootControlWrapper.Control;
            foreach (var subview in panel.Subviews)
            {
                subview.RemoveFromSuperview();
            }
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            UIView panel = _rootControlWrapper.Control;

            UINavigationBar navBar = new UINavigationBar(new RectangleF(0f, 0f, panel.Frame.Width, 42f));

            if (this.onBackCommand != null)
            {
                // Add a "Back" context and a delegate to handle the back command...
                //
                UINavigationItem navItemBack = new UINavigationItem("Back");
                navBar.PushNavigationItem(navItemBack, false);
                navBar.Delegate = new MaaasNavigationBarDelegate(this);
            }

            UINavigationItem navItem = new UINavigationItem(_pageTitle);
            navBar.PushNavigationItem(navItem, false);

            panel.AddSubview(navBar);

            // !!! What we really want to do here is create a (vertical) scrollview that covers the area from the 
            //     nav bar to the bottom of the screen.  Add any content to that scrollview.
            //
            /*
            JObject controlSpec = new JObject(
                new JProperty("type", "scrollview"),
                new JProperty("orientation", "vertical")
                // height (screen height - navBar.Bounds.Height) 
                // width (screen width)
            );

            var controlWrapper = CreateRootContainerControl(controlSpec);
            */

            if (content != null)
            {
                UIView childView = ((iOSControlWrapper)content).Control;
                RectangleF frame = childView.Frame;
                frame.Y = navBar.Bounds.Height;
                childView.Frame = frame;

                panel.AddSubview(childView);
            }
            _rootControlWrapper.ChildControls.Add(content);
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
            Util.debug("Message box with message: " + message);
        }
    }
}