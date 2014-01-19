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

namespace MaaasClientAndroid.Controls
{
    // http://theopentutorials.com/tutorials/android/listview/android-custom-listview-with-image-and-text-using-baseadapter/
    //
    // http://developer.android.com/reference/android/widget/Adapter.html
    //
 
    class ListItemView : TextView, ICheckable
    {
        bool _checked = false;

        public ListItemView(Context context)
            : base(context)
        {
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                Util.debug("Set checked: " + value);
                bool valueChanged = _checked != value;
                _checked = value;
                /*
                if (valueChanged)
                {
                    if (_checked)
                    {
                        this.SetBackgroundColor(Color.Aqua);
                    }
                    else
                    {
                        this.SetBackgroundColor(Color.Transparent);
                    }
                }
                 */
                this.RefreshDrawableState(); //?
            }
        }

        public void Toggle()
        {
            Util.debug("Toggle called");
            _checked = !_checked;
        }
    }


    class ListViewAdapter : BaseAdapter
    {
        Context _context;

        AndroidControlWrapper _parentControl;
        JObject _itemTemplate;
        List<BindingContext> _itemContexts;

        public ListViewAdapter(Context context, AndroidControlWrapper parentControl, JObject itemTemplate)
            : base()
        {
            _context = context;
            _parentControl = parentControl;
            _itemTemplate = itemTemplate;
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

                // By specifying IgnoreItemViewType we are telling the ListView not to recycle views (convertView will always be null).  It might be
                // nice to try to take advantage of view recycling, but I don't think that will work with the way our data binding works (the bound
                // values will attempt to update their associated views at various points in the future).
                //
                int viewType = ListViewAdapter.InterfaceConsts.IgnoreItemViewType;
                if (controlWrapper.Control.LayoutParameters != null)
                {
                    controlWrapper.Control.LayoutParameters = new ListView.LayoutParams(controlWrapper.Control.LayoutParameters.Width, controlWrapper.Control.LayoutParameters.Height, viewType);
                }
                else
                {
                    controlWrapper.Control.LayoutParameters = new ListView.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, viewType);
                }

                controlWrapper.Control.ViewDetachedFromWindow += Control_ViewDetachedFromWindow;

                // !!! OK, super creepy.  Usually the any bound views are created during an update cycle, at the end of which, the bindings update
                //     their views.  The problem here is that these views created by the adapter are created on-the-fly at some later point after
                //     the view has been already been updated based on the view model changes.  Calling UpdateViewFromViewModel() fixes this, but that's
                //     a pretty blunt solution (it will update everything in the view).  A more correct implementation might be to have some way of
                //     keeping track of bindings that have not been updated so they can be updated when signalled (here) - maybe marking the bindings
                //     as dirty, or possibly a method of resolving the bindings at the time the controls are created.
                //
                controlWrapper.ViewModel.UpdateViewFromViewModel();

                return controlWrapper.Control;

            }
            return null;
        }

        void Control_ViewDetachedFromWindow(object sender, View.ViewDetachedFromWindowEventArgs e)
        {
            Util.debug("View detached from window");
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
            // !!! Not really sure what the point of this is.  Presumably wrap actual content in a Java.Lang.Object,
            //     but then who's going to be processing that?
            return null;
        }

        public override long GetItemId(int position)
        {
            // !!!
            return position;
        }
    }

    class AndroidListViewWrapper : AndroidControlWrapper
    {
        public AndroidListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listview element");

            ListView listView = new ListView(((AndroidControlWrapper)parent).Control.Context);
            this._control = listView;

            ListViewAdapter adapter = new ListViewAdapter(((AndroidControlWrapper)parent).Control.Context, this, (JObject)controlSpec["itemTemplate"]);
            listView.Adapter = adapter;

            applyFrameworkElementDefaults(listView);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", new string[] { "onItemClick" });
            if (bindingSpec != null)
            {
                if (bindingSpec["items"] != null)
                {
                    string itemSelector = (string)bindingSpec["item"];
                    if (itemSelector == null)
                    {
                        itemSelector = "$data";
                    }

                    // This is a little unusual, but what the list view needs to update its contents is actually not the "value" (which would
                    // be the array of content items based on the "items" binding), but the binding context representing where those values are
                    // in the view model.  It needs the binding context in part so that it can apply itemSelector to the iterated bindings to
                    // produce the actual item binding contexts.  And of course it also needs the itemSelector value.  So we'll pass both of those
                    // in the delegate (and ignore "value" altogether).  This is all perfectly fine, it's just not the normal "jam the value into
                    // the control" kind of delegate, so it seemed prudent to note that here.
                    //
                    processElementBoundValue(
                        "items",
                        (string)bindingSpec["items"],
                        () => getListViewContents(listView),
                        value => this.setListViewContents(listView, GetValueBinding("items").BindingContext, itemSelector));
                }

                if (bindingSpec["selection"] != null)
                {
                    string selectionItem = (string)bindingSpec["selectionItem"];
                    if (selectionItem == null)
                    {
                        selectionItem = "$data";
                    }

                    processElementBoundValue(
                        "selection",
                        (string)bindingSpec["selection"],
                        () => getListViewSelection(listView, selectionItem),
                        value => this.setListViewSelection(listView, selectionItem, (JToken)value));
                }
            }

            // Get selection mode - None (default), Single, or Multiple - no dynamic values (we don't need this changing during execution).
            listView.ChoiceMode = ChoiceMode.None;
            if (controlSpec["select"] != null)
            {
                if ((string)controlSpec["select"] == "Single")
                {
                    listView.ChoiceMode = ChoiceMode.Single;
                }
                else if ((string)controlSpec["select"] == "Multiple")
                {
                    listView.ChoiceMode = ChoiceMode.Multiple;
                }
            }

            if (listView.ChoiceMode != ChoiceMode.None)
            {
                // !!! These don't really seem to get called (maybe only on kb or other non-touch interaction?)
                listView.ItemSelected += listView_ItemSelected;
                listView.NothingSelected += listView_NothingSelected;
            }

            /* Since we need to handle the item click in order to update the selection state anyway, we'll always
             * add the handler...
             * 
            ProcessCommands(bindingSpec, new string[] { "onItemClick" });
            if (GetCommand("onItemClick") != null)
            {
                listView.ItemClick += listView_ItemClick;
            }
             */
            listView.ItemClick += listView_ItemClick;

            // setListViewHeightBasedOnChildren();

            // adapter.NotifyDataSetChanged();
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

            ViewGroup.LayoutParams layout = listView.LayoutParameters;
            if (layout == null)
            {
                layout = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            }
            layout.Height = totalHeight + (listView.DividerHeight * (adapter.Count + 1));
            listView.LayoutParameters = layout;
            listView.RequestLayout();
        }

        void listView_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Util.debug("ListView item selected at position: " + e.Position);
            this.listView_SelectionChanged();
        }

        void listView_NothingSelected(object sender, AdapterView.NothingSelectedEventArgs e)
        {
            Util.debug("ListView nothing selected");
            this.listView_SelectionChanged();
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
            bool itemChecked = isListViewItemChecked(listView, e.Position);
            Util.debug("ListView item clicked, checked: " + itemChecked);
            this.listView_SelectionChanged();

            CommandInstance command = GetCommand("onItemClick");
            if (command != null)
            {
                Util.debug("ListView item click with command: " + command);

                // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                //
                ControlWrapper wrapper = this.getChildControlWrapper(e.View);
                if (wrapper != null)
                {
                    StateManager.processCommand(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                }
            }
        }

        public JToken getListViewContents(ListView listbox)
        {
            Util.debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(ListView listView, BindingContext bindingContext, string itemSelector)
        {
            Util.debug("Setting listview contents");

            ListViewAdapter adapter = (ListViewAdapter)listView.Adapter;
            adapter.SetContents(bindingContext, itemSelector);
            adapter.NotifyDataSetChanged();

            // This notification that the list backing this view has changed happens after the underlying bound values, and thus the list
            // view items themselves, have been updated.  We need to maintain the selection state, but that is difficult as items may
            // have "moved" list positions without any specific notification (other than this broad notification that the list itself changed).
            //
            // To address this, we get the "selection" binding for this list view, if any, and force a view update to reset the selection
            // state from the view model whenever the list bound to the list view changes (and after we've processed any adds/removes above).
            //
            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                selectionBinding.UpdateViewFromViewModel();
            }
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
            ListViewAdapter adapter = (ListViewAdapter)listView.Adapter;

            var checkedItems = listView.CheckedItemPositions;
            checkedItems.Clear();

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
                            checkedItems.Put(n, true);
                            break;
                        }
                    }
                }
                else
                {
                    if (JToken.DeepEquals(selection, bindingContext.Select(selectionItem).GetValue()))
                    {
                        checkedItems.Put(n, true);
                    }
                }
            }
        }

        void listView_SelectionChanged()
        {
            updateValueBindingForAttribute("selection");
        }
    }
}