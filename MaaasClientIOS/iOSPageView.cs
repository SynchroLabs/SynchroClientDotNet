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

    class PageContentScrollView : UIScrollView
    {
        public PageContentScrollView(RectangleF rect)
            : base(rect)
        {
        }

        public override void LayoutSubviews()
        {
            if (!Dragging && !Decelerating)
            {
                // Util.debug("Laying out sub view");

                SizeF size = new SizeF(this.ContentSize);

                // Size width to parent width (to achieve vertical-only scroll)
                size.Width = this.Superview.Frame.Width;

                foreach (UIView view in this.Subviews)
                {
                    // Size height of content area to height of contained views...
                    if ((view.Frame.Y + view.Frame.Height) > size.Height)
                    {
                        size.Height = view.Frame.Y + view.Frame.Height;
                    }
                }
                this.ContentSize = size;
            }

            base.LayoutSubviews();
        }
    }

    class iOSPageView : PageView
    {
        iOSControlWrapper _rootControlWrapper;
        PageContentScrollView _contentScrollView;
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
            // Add gesture recognizer to hide keyboard on tap
            var tap = new UITapGestureRecognizer { CancelsTouchesInView = false };
            tap.AddTarget(() => _rootControlWrapper.Control.EndEditing(true));
            tap.ShouldReceiveTouch += (recognizer, touch) => !(touch.View is UIButton);
            _rootControlWrapper.Control.AddGestureRecognizer(tap);
        }

        public void onKeyboardShown(NSNotification notification)
        {
            // May want animate this at some point - see: https://gist.github.com/redent/7263276
            //
            var keyboardFrame = UIKeyboard.FrameEndFromNotification(notification);
            Util.debug("Keyboard shown - frame: " + keyboardFrame);

            var contentInsets = new UIEdgeInsets(0.0f, 0.0f, keyboardFrame.Height, 0.0f);
            _contentScrollView.ContentInset = contentInsets;
            _contentScrollView.ScrollIndicatorInsets = contentInsets;

            centerScrollView();
        }

        public void onKeyboardHidden(NSNotification notification)
        {
            Util.debug("Keyboard hidden");
            _contentScrollView.ContentInset = UIEdgeInsets.Zero;
            _contentScrollView.ScrollIndicatorInsets = UIEdgeInsets.Zero;
        }

        public static UIView FindFirstResponder(UIView view)
        {
            if (view.IsFirstResponder)
            {
                return view;
            }

            foreach (UIView subView in view.Subviews)
            {
                var firstResponder = FindFirstResponder(subView);
                if (firstResponder != null)
                {
                    return firstResponder;
                }
            }
            return null;
        }

        // Center the scroll view on the active edit control.
        //
        public void centerScrollView()
        {
            // We could use this any time the edit control focus changed, on the "return" in an edit control, etc.
            //
            UIView activeView = FindFirstResponder(_contentScrollView);
            if (activeView != null)
            {
                RectangleF activeViewRect = activeView.Superview.ConvertRectToView(activeView.Frame, _contentScrollView);

                float scrollAreaHeight = _contentScrollView.Frame.Height - _contentScrollView.ContentInset.Bottom;

                var offset = Math.Max(0, activeViewRect.Y - (scrollAreaHeight - activeView.Frame.Height) / 2);
                _contentScrollView.SetContentOffset(new PointF(0, offset), false);
            }
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
            _contentScrollView = null;
        }

        public override void SetContent(ControlWrapper content)
        {
            UIView panel = _rootControlWrapper.Control;

            // Create the nav bar, add a back control as appropriate...
            //
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

            // Create the main content area (scroll view) and add the page content to it...
            //
            _contentScrollView = new PageContentScrollView(new RectangleF(0f, navBar.Bounds.Height, panel.Frame.Width, panel.Frame.Height - navBar.Bounds.Height));
            panel.AddSubview(_contentScrollView);
            if (content != null)
            {
                UIView childView = ((iOSControlWrapper)content).Control;
                _contentScrollView.AddSubview(childView);
                // We're adding the content to the _rootControlWrapper child list, even thought the scroll view
                // is actually in between (in the view heirarchy) - but that shouldn't be a problem.
                _rootControlWrapper.ChildControls.Add(content);
            }
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
            Util.debug("Message box with message: " + message);

            UIAlertView alertView = new UIAlertView();

            alertView.Message = message;
            if (messageBox["title"] != null)
            {
                alertView.Title = PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext);
            }

            List<string> buttonCommands = new List<string>();

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    alertView.AddButton(PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext));
                    string command = null;
                    if (option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }
                    buttonCommands.Add(command);
                }
            }
            else
            {
                alertView.AddButton("Close");
                buttonCommands.Add(null);
            }

            alertView.Clicked += (s, b) =>
            {
                Util.debug("Button " + b.ButtonIndex.ToString() + " clicked");
                if (buttonCommands[b.ButtonIndex] != null)
                {
                    string command = buttonCommands[b.ButtonIndex];
                    Util.debug("MessageBox command: " + command);
                    _stateManager.processCommand(command);
                }
            };

            alertView.Show();
        }
    }
}