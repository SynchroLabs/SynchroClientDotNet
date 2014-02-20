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

    public class iOSPageView : PageView
    {
        iOSControlWrapper _rootControlWrapper;
        PageContentScrollView _contentScrollView;
        string _pageTitle = "";

        UIBarButtonItem _navBarButton;
        List<UIBarButtonItem> _toolBarButtons = new List<UIBarButtonItem>();

        public iOSPageView(StateManager stateManager, ViewModel viewModel, UIView panel) :
            base(stateManager, viewModel)
        {
            _rootControlWrapper = new iOSControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, panel);

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
            _navBarButton = null;
            _toolBarButtons.Clear();

            UIView panel = _rootControlWrapper.Control;
            foreach (var subview in panel.Subviews)
            {
                subview.RemoveFromSuperview();
            }
            _rootControlWrapper.ChildControls.Clear();
            _contentScrollView = null;
        }

        public void SetNavBarButton(UIBarButtonItem button)
        {
            _navBarButton = button;
        }

        public void AddToolbarButton(UIBarButtonItem button)
        {
            _toolBarButtons.Add(button);
        }

        public override void SetContent(ControlWrapper content)
        {
            UIView panel = _rootControlWrapper.Control;

            RectangleF contentRect = new RectangleF(0f, 0f, panel.Frame.Width, panel.Frame.Height);

            // Create the nav bar, add a back control as appropriate...
            //
            UINavigationBar navBar = new UINavigationBar();
            navBar.SizeToFit();

            if (this.onBackCommand != null)
            {
                // Add a "Back" context and a delegate to handle the back command...
                //
                UINavigationItem navItemBack = new UINavigationItem("Back");
                navBar.PushNavigationItem(navItemBack, false);
                navBar.Delegate = new MaaasNavigationBarDelegate(this);
            }

            UINavigationItem navItem = new UINavigationItem(_pageTitle);

            if (_navBarButton != null)
            {
                navItem.SetRightBarButtonItem(_navBarButton, false);
            }

            navBar.PushNavigationItem(navItem, false);
            panel.AddSubview(navBar);

            // Adjust content rect based on navbar.
            //
            contentRect = new RectangleF(contentRect.Left, contentRect.Top + navBar.Bounds.Height, contentRect.Width, contentRect.Height - navBar.Bounds.Height);

            UIToolbar toolBar = null;
            if (_toolBarButtons.Count > 0)
            {
                // Create toolbar, position it at the bottom of the screen, adjust content rect to represent remaining space
                //
                toolBar = new UIToolbar() { BarStyle = UIBarStyle.Default };
                toolBar.SizeToFit();
                toolBar.Frame = new RectangleF(contentRect.Left, contentRect.Top + contentRect.Height - toolBar.Frame.Height, contentRect.Width, toolBar.Frame.Height);
                contentRect = new RectangleF(contentRect.Left, contentRect.Top, contentRect.Width, contentRect.Height - toolBar.Bounds.Height);

                // Create a new colection of toolbar buttons with flexible space surrounding and between them, then add to toolbar
                //
                var flexibleSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace);
                List<UIBarButtonItem> formattedItems = new List<UIBarButtonItem>();
                formattedItems.Add(flexibleSpace);
                foreach (UIBarButtonItem buttonItem in _toolBarButtons)
                {
                    formattedItems.Add(buttonItem);
                    formattedItems.Add(flexibleSpace);
                }
                toolBar.Items = formattedItems.ToArray();

                panel.AddSubview(toolBar);
            }

            // Create the main content area (scroll view) and add the page content to it...
            //
            _contentScrollView = new PageContentScrollView(contentRect);
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