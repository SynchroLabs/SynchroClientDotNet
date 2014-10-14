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
using Newtonsoft.Json.Linq;
using Android.Graphics;

namespace SynchroClientAndroid.Controls
{
    class ListItemView : RelativeLayout, ICheckable
    {
        View _contentView;
        bool _checkable;
        CheckBox _checkBox = null;

        public ListItemView(Context context, View contentView, int viewType, bool checkable)
            : base(context)
        {
            _contentView = contentView;
            _checkable = checkable;

            this.LayoutParameters = new ListView.LayoutParams(ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.WrapContent, viewType);

            RelativeLayout.LayoutParams contentLayoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            contentLayoutParams.AddRule(LayoutRules.CenterVertical);
            this.AddView(contentView, contentLayoutParams);

            if (_checkable)
            {
                // If you add any view that is focusable inside of a ListView row, it will make the row un-selectabled. 
                // For more, see: http://wiresareobsolete.com/wordpress/2011/08/clickable-zones-in-listview-items/
                //
                // Turns out we don't want the checkbox to be clickable (or focusable) anyway, so no problemo.
                //
                _checkBox = new CheckBox(this.Context);
                _checkBox.Clickable = false;
                _checkBox.Focusable = false;
                RelativeLayout.LayoutParams checkboxLayoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                checkboxLayoutParams.AddRule(LayoutRules.AlignParentRight);
                checkboxLayoutParams.AddRule(LayoutRules.CenterVertical);
                this.AddView(_checkBox, checkboxLayoutParams);
            }
        }

        public View ContentView { get { return _contentView; } }

        public bool Checked
        {
            get { return _checkBox.Checked; }
            set { _checkBox.Checked = value; }
        }

        public void Toggle()
        {
            _checkBox.Toggle();
        }
    }

    class ListViewAdapter : BaseAdapter
    {
        static Logger logger = Logger.GetLogger("ListViewAdapter");

        Context _context;

        AndroidControlWrapper _parentControl;
        JObject _itemTemplate;
        List<BindingContext> _itemContexts;
        bool _checkable;

        public ListViewAdapter(Context context, AndroidControlWrapper parentControl, JObject itemTemplate, Boolean checkable)
            : base()
        {
            _context = context;
            _parentControl = parentControl;
            _itemTemplate = itemTemplate;
            _checkable = checkable;
        }

        public void SetContents(BindingContext bindingContext, string itemSelector)
        {
            _itemContexts = bindingContext.SelectEach(itemSelector);
        }

        public List<BindingContext> BindingContexts { get { return _itemContexts; } }

        public BindingContext GetBindingContext(int position)
        {
            if ((_itemContexts != null) && (_itemContexts[position] != null))
            {
                return _itemContexts[position];
            }

            return null;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if ((_itemContexts != null) && (_itemContexts[position] != null))
            {
                AndroidControlWrapper controlWrapper = AndroidControlWrapper.CreateControl(_parentControl, _itemContexts[position], _itemTemplate);

                // !!! Need some kind of unregister strategy (when rows get shitcanned by the view).  Maybe:
                //
                //         controlWrapper.Control.ViewDetachedFromWindow += Control_ViewDetachedFromWindow;

                // By specifying IgnoreItemViewType we are telling the ListView not to recycle views (convertView will always be null).  It might be
                // nice to try to take advantage of view recycling, but that presents somewhat of a challenge in terms of the way our data binding 
                // works (the bound values will attempt to update their associated views at various points in the future).
                //
                int viewType = ListViewAdapter.InterfaceConsts.IgnoreItemViewType;

                ListItemView listItemView = new ListItemView(parent.Context, controlWrapper.Control, viewType, _checkable);
                return listItemView;
            }
            return null;
        }

        void Control_ViewDetachedFromWindow(object sender, View.ViewDetachedFromWindowEventArgs e)
        {
            logger.Debug("View detached from window");
        }

        public override int Count 
        { 
            get 
            {
                if (_itemContexts != null)
                {
                    return _itemContexts.Count;
                }
                return 0;
            } 
        }

        public override Java.Lang.Object GetItem(int position)
        {
            // Not really sure what the point of this is.  Presumably wrap actual content in a Java.Lang.Object, but
            // then who's going to be processing that?  I've never seen this method actually get called in practice.
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }
    }

    class AndroidListViewWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidListViewWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick, CommandName.OnSelectionChange };

        public AndroidListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating listview element");

            ListView listView = new ListView(((AndroidControlWrapper)parent).Control.Context);
            this._control = listView;

            ChoiceMode choiceMode = ChoiceMode.None;

            ListSelectionMode mode = ToListSelectionMode(controlSpec["select"]);
            switch (mode)
            {
                case ListSelectionMode.Single:
                    choiceMode = ChoiceMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    choiceMode = ChoiceMode.Multiple;
                    break;
            }

            ListViewAdapter adapter = new ListViewAdapter(((AndroidControlWrapper)parent).Control.Context, this, (JObject)controlSpec["itemTemplate"], choiceMode != ChoiceMode.None);
            listView.Adapter = adapter;

            listView.ChoiceMode = choiceMode;

            applyFrameworkElementDefaults(listView);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListViewContents(listView),
                    value => this.setListViewContents(listView, GetValueBinding("items").BindingContext));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListViewSelection(listView, selectionItem),
                    value => this.setListViewSelection(listView, selectionItem, (JToken)value));
            }

            if (listView.ChoiceMode != ChoiceMode.None)
            {
                // Have not witnessed these getting called (maybe they get called on kb or other non-touch interaction?).
                // At any rate, if there is any change to the selection, we need to know about it, and may need to add these,
                // but not safe to do so until we have an environment where they are testable.
                //
                // listView.ItemSelected += listView_ItemSelected;
                // listView.NothingSelected += listView_NothingSelected;
            }

            // Since we need to handle the item click in order to update the selection state anyway, we'll always add
            // the handler (whether or not there is an onItemClick command, which it will also handle if present)...
            //
            listView.ItemClick += listView_ItemClick;

            // setListViewHeightBasedOnChildren();
        }

        public void setListViewHeightBasedOnChildren()
        {
            ListView listView = (ListView)_control;
            IListAdapter adapter = listView.Adapter;
            if (adapter == null)
            {
                return;
            }

            int totalHeight = 0;
            for (int i = 0; i < adapter.Count; i++)
            {
                View listItem = adapter.GetView(i, null, listView);
                int measureSpec = Android.Views.View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
                listItem.Measure(0, measureSpec);
                totalHeight += listItem.MeasuredHeight + ((CheckedTextView)listItem).TotalPaddingBottom + ((CheckedTextView)listItem).TotalPaddingTop;
            }

            _height = totalHeight + (listView.DividerHeight * (adapter.Count + 1));
            this.updateSize();
        }

        public JToken getListViewContents(ListView listbox)
        {
            logger.Debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(ListView listView, BindingContext bindingContext)
        {
            logger.Debug("Setting listview contents");

            _selectionChangingProgramatically = true;

            ListViewAdapter adapter = (ListViewAdapter)listView.Adapter;
            adapter.SetContents(bindingContext, "$data");
            adapter.NotifyDataSetChanged();

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                selectionBinding.UpdateViewFromViewModel();
            }
            else if (_localSelection != null)
            {
                // If there is not a "selection" value binding, then we use local selection state to restore the selection when
                // re-filling the list.
                //
                this.setListViewSelection(listView, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        // To determine if an item should be selected, get an item from the list, get the ElementMetaData.BindingContext.  Apply any
        // selectionItem to the binding context, resolve that and compare it to the selection (selectionItem will always be provided
        // here, and will default to "$data").
        //
        public JToken getListViewSelection(ListView listView, string selectionItem)
        {
            ListViewAdapter adapter = (ListViewAdapter)listView.Adapter;

            List<BindingContext> selectedBindingContexts = new List<BindingContext>();
            var checkedItems = listView.CheckedItemPositions;
            for (var i = 0; i < checkedItems.Size(); i++)
            {
                int key = checkedItems.KeyAt(i);
                if (checkedItems.Get(key))
                {
                    selectedBindingContexts.Add(adapter.GetBindingContext(key));
                }
            }

            if (listView.ChoiceMode == ChoiceMode.Multiple)
            {
                return new JArray(
                    from BindingContext bindingContext in selectedBindingContexts
                    select bindingContext.Select(selectionItem).GetValue()
                );
            }
            else if (listView.ChoiceMode == ChoiceMode.Single)
            {
                if (selectedBindingContexts.Count > 0)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return selectedBindingContexts[0].Select(selectionItem).GetValue().DeepClone();
                }
                return new Newtonsoft.Json.Linq.JValue(false); // This is a "null" selection
            }

            return null;
        }

        // This gets triggered when selection changes come in from the server (including when the selection is initially set),
        // and it also gets triggered when the list itself changes (including when the list contents are intially set).  So 
        // in the initial list/selection set case, this gets called twice.  On subsequent updates it's possible that this will
        // be triggered by either a list change or a selection change from the server, or both.  There is no easy way currerntly
        // to detect the "both" case (without exposing a lot more information here).  We're going to go ahead and live with the
        // multiple calls.  It shouldn't hurt anything (they should produce the same result), it's just slightly inefficient.
        // 
        public void setListViewSelection(ListView listView, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            ListViewAdapter adapter = (ListViewAdapter)listView.Adapter;

            listView.ClearChoices();

            for (int n = 0; n < adapter.Count; n++)
            {
                BindingContext bindingContext = adapter.GetBindingContext(n);
                if (selection is JArray)
                {
                    JArray array = selection as JArray;
                    foreach (JToken item in array.Children())
                    {
                        if (JToken.DeepEquals(item, bindingContext.Select(selectionItem).GetValue()))
                        {
                            listView.SetItemChecked(n, true);
                            break;
                        }
                    }
                }
                else
                {
                    if (JToken.DeepEquals(selection, bindingContext.Select(selectionItem).GetValue()))
                    {
                        listView.SetItemChecked(n, true);
                    }
                }
            }

            _selectionChangingProgramatically = false;
        }

        static bool isListViewItemChecked(ListView listView, int position)
        {
            bool isChecked = false;
            if (listView.ChoiceMode == ChoiceMode.Single)
            {
                isChecked = position == listView.CheckedItemPosition;
            }
            else if (listView.ChoiceMode == ChoiceMode.Multiple)
            {
                isChecked = listView.CheckedItemPositions.ValueAt(position);
            }
            return isChecked;
        }

        void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ListView listView = (ListView)this.Control;

            if (listView.ChoiceMode != ChoiceMode.None)
            {
                ValueBinding selectionBinding = GetValueBinding("selection");
                if (selectionBinding != null)
                {
                    updateValueBindingForAttribute("selection");
                }
                else if (!_selectionChangingProgramatically)
                {
                    _localSelection = this.getListViewSelection(listView, "$data");
                }

            }

            if (!_selectionChangingProgramatically)
            {
                // Process commands
                //
                if (listView.ChoiceMode == ChoiceMode.None)
                {
                    CommandInstance command = GetCommand(CommandName.OnItemClick);
                    if (command != null)
                    {
                        logger.Debug("ListView item clicked with command: {0}", command);

                        ListItemView listItemView = e.View as ListItemView;
                        if (listItemView != null)
                        {
                            // The item click command handler resolves its tokens relative to the item clicked.
                            //
                            View contentView = listItemView.ContentView;
                            ControlWrapper wrapper = this.getChildControlWrapper(contentView);
                            if (wrapper != null)
                            {
                                StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                            }
                        }
                    }
                }
                else
                {
                    CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                    if (command != null)
                    {
                        logger.Debug("ListView selection changed with command: {0}", command);

                        if (listView.ChoiceMode == ChoiceMode.Single)
                        {
                            ListItemView listItemView = e.View as ListItemView;
                            if (listItemView != null)
                            {
                                // The selection change command handler resolves its tokens relative to the item selected when in single select mode.
                                //
                                View contentView = listItemView.ContentView;
                                ControlWrapper wrapper = this.getChildControlWrapper(contentView);
                                if (wrapper != null)
                                {
                                    StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                                }
                            }
                        }
                        else // ChoiceMode.Multiple
                        {
                            // The selection change command handler resolves its tokens relative to the list context when in multiple select mode.
                            //
                            StateManager.processCommand(command.Command, command.GetResolvedParameters(this.BindingContext));
                        }
                    }
                }
            }
        }
    }
}