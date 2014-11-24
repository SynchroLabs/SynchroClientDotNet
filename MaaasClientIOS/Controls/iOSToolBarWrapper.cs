using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Threading.Tasks;

namespace MaaasClientIOS.Controls
{
    class iOSToolBarWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSToolBarWrapper");

        static string[] Commands = new string[] { CommandName.OnClick };

        public iOSToolBarWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating tool bar button element");

            UIBarButtonItem buttonItem = null;

            // Note: Because the navBar button does not coerce/stlye images, it is recommended that you use either a systemItem or text-only
            //       button element on the navBar (especially important for correct styling across different versions of iOS).
            //

            if (controlSpec["systemItem"] != null)
            {
                // System items:                         
                //
                //     Done, Cancel, Edit, Save, Add, Compose, Reply, Action, Organize, Bookmarks, Search, Refresh, Stop, 
                //     Camera, Trash, Play, Pause, Rewind, FastForward, Undo, Redo, PageCurl
                //
                //     https://developer.apple.com/library/ios/documentation/uikit/reference/UIBarButtonItem_Class/Reference/Reference.html
                //
                UIBarButtonSystemItem item = (UIBarButtonSystemItem)typeof(UIBarButtonSystemItem).GetField((string)controlSpec["systemItem"]).GetValue(null);
                buttonItem = new UIBarButtonItem(item, buttonItem_Clicked);
            }
            else
            {
                // Custom items, can specify text, icon, or both
                //
                buttonItem = new UIBarButtonItem("", UIBarButtonItemStyle.Plain, buttonItem_Clicked);
                processElementProperty(controlSpec["text"], value => buttonItem.Title = ToString(value));
                processElementProperty(controlSpec["icon"], value => buttonItem.Image = UIImage.FromBundle("icons/blue/" + ToString(value)));
            }
            
            processElementProperty(controlSpec["enabled"], value => buttonItem.Enabled = ToBoolean(value));

            if ((string)controlSpec["control"] == "navBar.button")
            {
                // When image and text specified, uses image.  Image is placed on button surface verbatim (no color coersion).
                //
                _pageView.SetNavBarButton(buttonItem);
            }
            else // toolBar.button
            {
                // Can use image, text, or both, and toolbar shows what was provided (including image+text).  Toolbar coerces colors
                // and handles disabled state (for example, on iOS 6, icons/text show up as white when enabled and gray when disabled).
                //
                _pageView.AddToolbarButton(buttonItem);
            }

            _isVisualElement = false;

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnClick, Commands);
            ProcessCommands(bindingSpec, Commands);
        }

        async void buttonItem_Clicked(object sender, EventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnClick);
            if (command != null)
            {
                logger.Debug("Button click with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}