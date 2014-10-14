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
        static Logger logger = Logger.GetLogger("MaaasNavigationBarDelegate");

        iOSPageView _pageView;

        public MaaasNavigationBarDelegate(iOSPageView pageView)
        {
            _pageView = pageView;
        }

        // Per several recommendations, especially this one: http://blog.falafel.com/ios-7-bars-with-xamarinios/
        //
        // !!! This is supposed to fix the Navbar positioning on iOS7, but doesn't do anything for us...
        //
        public override UIBarPosition GetPositionForBar(IUIBarPositioning barPositioning)
        {
            return UIBarPosition.TopAttached;
        }

        public override bool ShouldPopItem(UINavigationBar navigationBar, UINavigationItem item)
        {
            logger.Debug("Should pop item got called!");
            if (_pageView != null)
            {
                _pageView.OnBackCommand();
            }
            return false;
        }
    }

    class PageContentScrollView : UIScrollView
    {
        iOSControlWrapper _content;

        public PageContentScrollView(RectangleF rect, iOSControlWrapper content)
            : base(rect)
        {
            _content = content;
            if (_content != null)
            {
                this.AddSubview(_content.Control);
            }
        }

        public override void LayoutSubviews()
        {
            if (!Dragging && !Decelerating)
            {
                // Util.debug("Laying out sub view");
                if (_content != null)
                {
                    // Size child (content) to parent as appropriate
                    //
                    RectangleF frame = _content.Control.Frame;

                    if (_content.FrameProperties.HeightSpec == SizeSpec.FillParent)
                    {
                        frame.Height = this.Frame.Height;
                    }

                    if (_content.FrameProperties.WidthSpec == SizeSpec.FillParent)
                    {
                        frame.Width = this.Frame.Width;
                    }

                    _content.Control.Frame = frame;

                    // Set scroll content area based on size of contents
                    //
                    SizeF size = new SizeF(this.ContentSize);

                    // Size width of scroll content area to container width (to achieve vertical-only scroll)
                    size.Width = this.Superview.Frame.Width;

                    // Size height of scroll content area to height of contained views...
                    size.Height = _content.Control.Frame.Y + _content.Control.Frame.Height;

                    this.ContentSize = size;
                }
            }

            base.LayoutSubviews();
        }
    }

    public class iOSPageView : PageView
    {
        static Logger logger = Logger.GetLogger("iOSPageView"); 
        
        string _pageTitle = "";

        iOSControlWrapper _rootControlWrapper;
        PageContentScrollView _contentScrollView;

        UINavigationBar _navBar;
        UIBarButtonItem _navBarButton;

        UIToolbar _toolBar;
        List<UIBarButtonItem> _toolBarButtons = new List<UIBarButtonItem>();

        public iOSPageView(StateManager stateManager, ViewModel viewModel, UIView panel, Action doBackToMenu = null) :
            base(stateManager, viewModel, doBackToMenu)
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
            logger.Debug("Keyboard shown - frame: {0}", keyboardFrame);

            var contentInsets = new UIEdgeInsets(0.0f, 0.0f, keyboardFrame.Height, 0.0f);
            _contentScrollView.ContentInset = contentInsets;
            _contentScrollView.ScrollIndicatorInsets = contentInsets;

            centerScrollView();
        }

        public void onKeyboardHidden(NSNotification notification)
        {
            logger.Debug("Keyboard hidden");
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

        // !!! The ContentTop and SizeNavBar methods below are a pretty ugly hack to address the issues with
        //     navigation bar sizing/positioning in iOS7.  iOS7 is supposed to magically handle all of this, and if
        //     not, then NavigationBarDelegate.GetPositionForBar() fix is supposed to do the job, but it does not
        //     in our case.  I assume this is because we create our navbar on the fly, after the ViewController is
        //     created.  I tried a number of ways to get this to work across iOS 6 and 7 without checking the version
        //     number and using a hardcoded status bar height, but was not able to make it work.
        //
        public static float ContentTop
        {
            get 
            {
                if (iOSUtil.IsiOS7)
                {
                    return 20f; // Height of status bar in iOS7
                }

                return 0f;
            }
        }

        public static void SizeNavBar(UINavigationBar navBar)
        {
            navBar.SizeToFit();
            if (iOSUtil.IsiOS7)
            {
                navBar.Frame = new RectangleF(navBar.Frame.X, ContentTop, navBar.Frame.Width, navBar.Frame.Height);
            }
        }

        public void UpdateLayout()
        {
            // Equivalent in concept to LayoutSubviews (but renamed to avoid confusion, since PageView isn't a UIView)
            //
            UIView panel = _rootControlWrapper.Control;

            RectangleF contentRect = new RectangleF(0f, ContentTop, panel.Frame.Width, panel.Frame.Height - ContentTop);

            if (_navBar != null)
            {
                SizeNavBar(_navBar);
                contentRect = new RectangleF(contentRect.X, contentRect.Y + _navBar.Frame.Height, contentRect.Width, contentRect.Height - _navBar.Frame.Height);
            }

            if (_toolBar != null)
            {
                _toolBar.SizeToFit();
                _toolBar.Frame = new RectangleF(contentRect.Left, contentRect.Top + contentRect.Height - _toolBar.Frame.Height, contentRect.Width, _toolBar.Frame.Height);
                contentRect = new RectangleF(contentRect.Left, contentRect.Top, contentRect.Width, contentRect.Height - _toolBar.Bounds.Height);
            }

            _contentScrollView.Frame = contentRect;
        }

        public override void ClearContent()
        {
            _navBar = null;
            _navBarButton = null;

            _toolBar = null;
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

            RectangleF contentRect = new RectangleF(0f, ContentTop, panel.Frame.Width, panel.Frame.Height - ContentTop);

            // Create the nav bar, add a back control as appropriate...
            //
            _navBar = new UINavigationBar();
            _navBar.Delegate = new MaaasNavigationBarDelegate(this);
            SizeNavBar(_navBar);

            if (this.HasBackCommand)
            {
                // Add a "Back" context and a delegate to handle the back command...
                //
                UINavigationItem navItemBack = new UINavigationItem("Back");
                _navBar.PushNavigationItem(navItemBack, false);
            }

            UINavigationItem navItem = new UINavigationItem(_pageTitle);

            if (_navBarButton != null)
            {
                navItem.SetRightBarButtonItem(_navBarButton, false);
            }

            _navBar.PushNavigationItem(navItem, false);
            panel.AddSubview(_navBar);

            // Adjust content rect based on navbar.
            //
            contentRect = new RectangleF(contentRect.Left, contentRect.Top + _navBar.Bounds.Height, contentRect.Width, contentRect.Height - _navBar.Bounds.Height);

            _toolBar = null;
            if (_toolBarButtons.Count > 0)
            {
                // Create toolbar, position it at the bottom of the screen, adjust content rect to represent remaining space
                //
                _toolBar = new UIToolbar() { BarStyle = UIBarStyle.Default };
                _toolBar.SizeToFit();
                _toolBar.Frame = new RectangleF(contentRect.Left, contentRect.Top + contentRect.Height - _toolBar.Frame.Height, contentRect.Width, _toolBar.Frame.Height);
                contentRect = new RectangleF(contentRect.Left, contentRect.Top, contentRect.Width, contentRect.Height - _toolBar.Bounds.Height);

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
                _toolBar.Items = formattedItems.ToArray();

                panel.AddSubview(_toolBar);
            }

            // Create the main content area (scroll view) and add the page content to it...
            //
            _contentScrollView = new PageContentScrollView(contentRect, (iOSControlWrapper)content);
            panel.AddSubview(_contentScrollView);
            if (content != null)
            {
                // We're adding the content to the _rootControlWrapper child list, even thought the scroll view
                // is actually in between (in the view heirarchy) - but that shouldn't be a problem.
                _rootControlWrapper.ChildControls.Add(content);
            }
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox, CommandHandler onCommand)
        {
            string message = PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext);
            logger.Debug("Message box with message: {0}", message);

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
                logger.Debug("Button {0} clicked", b.ButtonIndex.ToString());
                if (buttonCommands[b.ButtonIndex] != null)
                {
                    string command = buttonCommands[b.ButtonIndex];
                    logger.Debug("MessageBox command: {0}", command);
                    onCommand(command);
                }
            };

            alertView.Show();
        }
    }
}