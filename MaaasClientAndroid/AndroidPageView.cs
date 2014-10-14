using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using SynchroClientAndroid.Controls;
using Newtonsoft.Json.Linq;
using Android.Util;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace SynchroClientAndroid
{
    public class AndroidActionBarItem
    {
        protected Context _context;

        protected string _title;
        protected string _iconName;
        protected int _iconResourceId;
        protected Drawable _icon;
        protected Drawable _iconDisabled;
        protected bool _enabled = true;
        protected Action _onItemSelected;
        protected ShowAsAction _showAsAction = ShowAsAction.Never;  // [Always, Never, IfRoom] | WithText
        protected IMenuItem _menuItem;

        public AndroidActionBarItem(Context context)
        {
            _context = context;
        }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
            }
        }

        public ShowAsAction ShowAsAction 
        { 
            get { return _showAsAction; } 
            set 
            {
                _showAsAction = value; 
                if (_menuItem != null)
                {
                    _menuItem.SetShowAsAction(_showAsAction);
                }
            } 
        }

        protected void updateIconOnMenuItem()
        {
            if ((_menuItem != null) && (_iconResourceId > 0))
            {
                if (_enabled)
                {
                    if (_menuItem.Icon != _icon)
                    {
                        _menuItem.SetIcon(_icon);
                    }
                }
                else
                {
                    if (_iconDisabled == null)
                    {
                        // !!! This is probably not the best way to show the icon as disabled, but it works for now...
                        _iconDisabled = _context.Resources.GetDrawable(_iconResourceId);
                        _iconDisabled.Mutate().SetColorFilter(Color.Gray, PorterDuff.Mode.SrcIn);
                    }

                    if (_menuItem.Icon != _iconDisabled)
                    {
                        _menuItem.SetIcon(_iconDisabled);
                    }
                }
            }
        }

        public string Icon
        {
            get { return _iconName; }
            set
            {
                _iconName = value;
                _iconResourceId = (int)typeof(Resource.Drawable).GetField(_iconName).GetValue(null);
                if (_iconResourceId > 0)
                {
                    _icon = _context.Resources.GetDrawable(_iconResourceId);
                }

                updateIconOnMenuItem();
            }
        }

        public bool IsEnabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (_menuItem != null)
                {
                    updateIconOnMenuItem();
                    _menuItem.SetEnabled(_enabled);
                }
            }
        }

        public Action OnItemSelected { get { return _onItemSelected; } set { _onItemSelected = value; } }

        public IMenuItem MenuItem 
        { 
            get { return _menuItem; } 
            set 
            {
                _menuItem = value;
                _menuItem.SetShowAsAction(_showAsAction);
                updateIconOnMenuItem();
                _menuItem.SetEnabled(_enabled);
            } 
        }
    }

    public class AndroidPageView  : PageView
    {
        static Logger logger = Logger.GetLogger("AndroidPageView");

        Activity _activity;
        AndroidControlWrapper _rootControlWrapper;

        List<AndroidActionBarItem> _actionBarItems = new List<AndroidActionBarItem>();

        public AndroidPageView(StateManager stateManager, ViewModel viewModel, Activity activity, ViewGroup panel, Action doBackToMenu = null) :
            base(stateManager, viewModel, doBackToMenu)
        {
            _activity = activity;
            _rootControlWrapper = new AndroidControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, panel);
        }

        public override ControlWrapper CreateRootContainerControl(JObject controlSpec)
        {
            return AndroidControlWrapper.CreateControl(_rootControlWrapper, _viewModel.RootBindingContext, controlSpec);
        }

        public AndroidActionBarItem CreateAndAddActionBarItem()
        {
            AndroidActionBarItem actionBarItem = new AndroidActionBarItem(_activity.ApplicationContext);
            this._actionBarItems.Add(actionBarItem);
            return actionBarItem;
        }

        public bool OnCreateOptionsMenu(IMenu menu)
        {
            logger.Debug("Option menu created");
            if (_actionBarItems.Count > 0)
            {
                int pos = 0;
                foreach(var actionBarItem in _actionBarItems)
                {
                    actionBarItem.MenuItem = menu.Add(0, pos, pos, actionBarItem.Title);
                    pos++;
                }
                return true;
            }
            else // No items
            {
                return false;
            }
        }

        public bool OnOptionsItemSelected(IMenuItem item)
        {
            if ((item.ItemId >= 0) && (item.ItemId < _actionBarItems.Count))
            {
                AndroidActionBarItem actionBarItem = _actionBarItems[item.ItemId];
                logger.Debug("Action bar item selected - id: {0}, title: {1}", item.ItemId, actionBarItem.Title);
                if (actionBarItem.OnItemSelected != null)
                {
                    actionBarItem.OnItemSelected();
                }
                return true;
            }

            return false;
        }

        public bool OnCommandBarUp(IMenuItem item)
        {
            logger.Debug("Command bar Up button pushed");
            this.OnBackCommand();
            return true;
        }

        public override void ClearContent()
        {
            this._actionBarItems.Clear();
            this._activity.InvalidateOptionsMenu();

            ViewGroup panel = (ViewGroup)_rootControlWrapper.Control;

            ScrollView mainScroller = panel as ScrollView;
            if (mainScroller != null)
            {
                mainScroller.ScrollTo(0, 0);
            }

            panel.RemoveAllViews(); 
            _rootControlWrapper.ChildControls.Clear();
        }

        public override void SetContent(ControlWrapper content)
        {
            AndroidControlWrapper controlWrapper = content as AndroidControlWrapper;

            // Default scroll behavior had the effect of allowing the contained item to grow
            // unbounded when its content was sized as "match parent" (or "*").  So for example,
            // if you had a vertical stack panel with a height of "*", which contained a vertical
            // wrap panel that also had a height of "*", once the wrap panel filled the space, it
            // would continue to expand (growing the scroll content) instead of wrapping to the 
            // scroll content area.  
            //
            // Since there is no way to disable scrolling in a ScrollView (at least not such that 
            // it will constrain its content size), we have to use a scroll view as the root only
            // when the contents is intended to scroll (meaning that the contents has an explicit
            // size or is sized as "wrap content".  Otherwise, we just use a stack panel that will
            // constrain the vertical size of the contents (and not scroll, obviously).
            //
            if (controlWrapper.Height == ViewGroup.LayoutParams.MatchParent)
            {
                if (_rootControlWrapper.Control is ScrollView)
                {
                    var newRootView = new LinearLayout(_activity);
                    _activity.SetContentView(newRootView);
                    _rootControlWrapper = new AndroidControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, newRootView);
                }
            }
            else
            {
                if (_rootControlWrapper.Control is LinearLayout)
                {
                    var newRootView = new ScrollView(_activity);
                    _activity.SetContentView(newRootView);
                    _rootControlWrapper = new AndroidControlWrapper(this, _stateManager, _viewModel, _viewModel.RootBindingContext, newRootView);
                }
            }

            ViewGroup panel = (ViewGroup)_rootControlWrapper.Control;
            if (content != null)
            {
                panel.AddView(((AndroidControlWrapper)content).Control);
            }
            _rootControlWrapper.ChildControls.Add(content);

            this._activity.ActionBar.SetDisplayHomeAsUpEnabled(this.HasBackCommand);
            this._activity.InvalidateOptionsMenu();
        }

        //
        // MessageBox stuff...
        //

        public override void ProcessMessageBox(JObject messageBox, CommandHandler onCommand)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(_activity);
            AlertDialog dialog = builder.Create();

            dialog.SetMessage(PropertyValue.ExpandAsString((string)messageBox["message"], _viewModel.RootBindingContext));
            if (messageBox["title"] != null)
            {
                dialog.SetTitle(PropertyValue.ExpandAsString((string)messageBox["title"], _viewModel.RootBindingContext));
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                if (options.Count > 0)
                {
                    JObject option = (JObject)options[0];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton(label, (s, ev) =>
                    {
                        logger.Debug("MessageBox Command invoked: {0}", label);
                        if (command != null)
                        {
                            logger.Debug("MessageBox command: {0}", command);
                            onCommand(command);
                        }
                    });
                }

                if (options.Count > 1)
                {
                    JObject option = (JObject)options[1];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton2(label, (s, ev) =>
                    {
                        logger.Debug("MessageBox Command invoked: {0}", label);
                        if (command != null)
                        {
                            logger.Debug("MessageBox command: {0}", command);
                            onCommand(command);
                        }
                    });
                }

                if (options.Count > 2)
                {
                    JObject option = (JObject)options[2];

                    string label = PropertyValue.ExpandAsString((string)option["label"], _viewModel.RootBindingContext);
                    string command = null;
                    if ((string)option["command"] != null)
                    {
                        command = PropertyValue.ExpandAsString((string)option["command"], _viewModel.RootBindingContext);
                    }

                    dialog.SetButton3(label, (s, ev) =>
                    {
                        logger.Debug("MessageBox Command invoked: {0}", label);
                        if (command != null)
                        {
                            logger.Debug("MessageBox command: {0}", command);
                            onCommand(command);
                        }
                    });
                }
            }
            else
            {
                // Not commands - add default "close"
                //
                dialog.SetButton("Close", (s, ev) =>
                {
                    logger.Debug("MessageBox default close button clicked");
                });
            }

            dialog.Show();
        }
    }
}