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
using Android.Graphics;
using SynchroCore;
using JValue = SynchroCore.JValue;
using System.Threading.Tasks;

namespace SynchroClientAndroid.Controls
{
    // http://docs.xamarin.com/guides/android/user_interface/working_with_listviews_and_adapters/part_3_-_customizing_a_listview%27s_appearance/
    //
    // Source for resources
    //
    //     https://github.com/android/platform_frameworks_base/tree/master/core/res/res/layout
    //

    public class BindingContextListboxAdapter : BaseAdapter
    {
        protected Context _context;
        protected IList<View> _views = new List<View>();

        protected List<BindingContextListItem> _listItems = new List<BindingContextListItem>();
        protected int _layoutResourceId;

        public BindingContextListboxAdapter(Context context, int itemLayoutResourceId)
        {
            _context = context;
            _layoutResourceId = itemLayoutResourceId;
        }

        public void SetContents(BindingContext bindingContext, string itemContent)
        {
            _listItems.Clear();

            List<BindingContext> itemBindingContexts = bindingContext.SelectEach("$data");
            foreach (BindingContext itemBindingContext in itemBindingContexts)
            {
                _listItems.Add(new BindingContextListItem(itemBindingContext, itemContent));
            }
        }

        public BindingContextListItem GetItemAtPosition(int position)
        {
            return _listItems.ElementAt(position);
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int id)
        {
            return id;
        }

        public override int Count
        {
            get
            {
                return _listItems.Count();
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            BindingContextListItem item = _listItems.ElementAt(position);

            var inflater = LayoutInflater.From(_context);
            var view = convertView ?? inflater.Inflate(_layoutResourceId, parent, false);

            var text = view.FindViewById<TextView>(Android.Resource.Id.Text1);
            if (text != null)
                text.Text = item.ToString();

            if (!_views.Contains(view))
                _views.Add(view);

            return view;
        }

        private void ClearViews()
        {
            foreach (var view in _views)
            {
                view.Dispose();
            }
            _views.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            ClearViews();
            base.Dispose(disposing);
        }
    }

    class AndroidListBoxWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidListBoxWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick.Attribute, CommandName.OnSelectionChange.Attribute };

        public AndroidListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating listbox element");

            ListView listView = new ListView(((AndroidControlWrapper)parent).Control.Context);
            this._control = listView;

            int listTemplate = Android.Resource.Layout.SimpleListItem1; // Default for ChoiceMode.None

            ListSelectionMode mode = ToListSelectionMode(controlSpec["select"]);
            switch (mode)
            {
                case ListSelectionMode.Single:
                    listTemplate = Android.Resource.Layout.SimpleListItemSingleChoice;
                    listView.ChoiceMode = ChoiceMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    listTemplate = Android.Resource.Layout.SimpleListItemMultipleChoice;
                    listView.ChoiceMode = ChoiceMode.Multiple;
                    break;
            }

            BindingContextListboxAdapter adapter = new BindingContextListboxAdapter(((AndroidControlWrapper)parent).Control.Context, listTemplate);
            listView.Adapter = adapter;
            
            setListViewHeightBasedOnChildren();

            applyFrameworkElementDefaults(listView);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items", 
                    (string)bindingSpec["items"], 
                    () => getListboxContents(listView),
                    value => this.setListboxContents(listView, GetValueBinding("items").BindingContext, itemContent));
            }
            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection", 
                    (string)bindingSpec["selection"], 
                    () => getListboxSelection(listView, selectionItem),
                    value => this.setListboxSelection(listView, selectionItem, (JToken)value));
            }

            listView.ItemClick += listView_ItemClick;
        }

        // !!! This doesn't really work at all.  When it does the measure pass, the value reported is significantly smaller than the size
        //     actually rendered.  Even if this did work, we'd want to obey height, minheight, and maxheight.
        //
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

        public JToken getListboxContents(ListView listView)
        {
            logger.Debug("Getting listbox contents");
            JArray array = new JArray();
            for (int n = 0; n < listView.Count; n++)
            {
                array.Add(new JValue(listView.GetItemAtPosition(n).ToString()));
            }
            return array;
        }

        public void setListboxContents(ListView listView, BindingContext bindingContext, string itemContent)
        {
            logger.Debug("Setting listbox contents");

            _selectionChangingProgramatically = true;

            BindingContextListboxAdapter adapter = (BindingContextListboxAdapter)listView.Adapter;
            adapter.SetContents(bindingContext, itemContent);
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
                this.setListboxSelection(listView, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getListboxSelection(ListView listView, string selectionItem)
        {
            BindingContextListboxAdapter adapter = (BindingContextListboxAdapter)listView.Adapter;

            List<BindingContextListItem> selectedListItems = new List<BindingContextListItem>();
            var checkedItems = listView.CheckedItemPositions;
            for (var i = 0; i < checkedItems.Size(); i++)
            {
                int key = checkedItems.KeyAt(i);
                if (checkedItems.Get(key))
                {
                    selectedListItems.Add(adapter.GetItemAtPosition(key));
                }
            }

            if (listView.ChoiceMode == ChoiceMode.Multiple)
            {
                JArray array = new JArray();
                foreach (BindingContextListItem listItem in selectedListItems)
                {
                    array.Add(listItem.GetSelection(selectionItem));
                }
                return array;
            }
            else if (listView.ChoiceMode == ChoiceMode.Single)
            {
                if (selectedListItems.Count > 0)
                {
                    return selectedListItems[0].GetSelection(selectionItem);
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        public void setListboxSelection(ListView listView, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            BindingContextListboxAdapter adapter = (BindingContextListboxAdapter)listView.Adapter;

            listView.ClearChoices();

            for (int n = 0; n < adapter.Count; n++)
            {
                BindingContextListItem listItem = adapter.GetItemAtPosition(n);
                if (selection is JArray)
                {
                    JArray array = selection as JArray;
                    foreach (JToken item in array)
                    {
                        if (JToken.DeepEquals(item, listItem.GetSelection(selectionItem)))
                        {
                            listView.SetItemChecked(n, true);
                            break;
                        }
                    }
                }
                else
                {
                    if (JToken.DeepEquals(selection, listItem.GetSelection(selectionItem)))
                    {
                        listView.SetItemChecked(n, true);
                    }
                }
            }

            _selectionChangingProgramatically = false;
        }

        async void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
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
                    _localSelection = this.getListboxSelection(listView, "$data");
                }

            }

            if (!_selectionChangingProgramatically)
            {
                // Process commands
                //
                BindingContextListboxAdapter adapter = (BindingContextListboxAdapter)listView.Adapter;

                if (listView.ChoiceMode == ChoiceMode.None)
                {
                    CommandInstance command = GetCommand(CommandName.OnItemClick);
                    if (command != null)
                    {
                        logger.Debug("ListView item clicked with command: {0}", command);

                        // The item click command handler resolves its tokens relative to the item clicked.
                        //
                        BindingContextListItem listItem = adapter.GetItemAtPosition(e.Position);
                        if (listItem != null)
                        {
                            await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(listItem.BindingContext));
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
                            // The selection change command handler resolves its tokens relative to the item selected when in single select mode.
                            //
                            BindingContextListItem listItem = adapter.GetItemAtPosition(e.Position);
                            if (listItem != null)
                            {
                                await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                            }
                        }
                        else // ChoiceMode.Multiple
                        {
                            // The selection change command handler resolves its tokens relative to the list context when in multiple select mode.
                            //
                            await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(this.BindingContext));
                        }
                    }
                }
            }
        }
    }
}