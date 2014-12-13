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
using System.Threading.Tasks;

namespace SynchroClientAndroid.Controls
{
    public class BindingContextListItem
    {
        BindingContext _bindingContext;
        string _itemContent;

        public BindingContextListItem(BindingContext bindingContext, string itemContent)
        {
            _bindingContext = bindingContext;
            _itemContent = itemContent;
        }

        public BindingContext BindingContext { get { return _bindingContext; } }

        public override string ToString()
        {
            return PropertyValue.ExpandAsString(_itemContent, _bindingContext);
        }

        public JToken GetValue()
        {
            return _bindingContext.Select("$data").GetValue();
        }

        public JToken GetSelection(string selectionItem)
        {
            return _bindingContext.Select(selectionItem).GetValue().DeepClone();
        }
    }

    public class BindingContextPickerAdapter : BaseAdapter, ISpinnerAdapter
    {
        protected Context _context;
        protected IList<View> _views = new List<View>(); 
        
        protected List<BindingContextListItem> _listItems = new List<BindingContextListItem>();

        public BindingContextPickerAdapter(Context context)
        {
            _context = context;
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

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            return GetCustomView(position, convertView, parent, true);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            return GetCustomView(position, convertView, parent, false);
        }

        private View GetCustomView(int position, View convertView, ViewGroup parent, bool dropdown)
        {
            BindingContextListItem item = _listItems.ElementAt(position);

            var inflater = LayoutInflater.From(_context);
            var view = convertView ?? inflater.Inflate((dropdown ? Android.Resource.Layout.SimpleSpinnerDropDownItem : Android.Resource.Layout.SimpleSpinnerItem), parent, false);

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

    // Android "Spinner" - http://developer.android.com/guide/topics/ui/controls/spinner.html
    //
    class AndroidPickerWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidPickerWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        int _lastProgramaticallySelectedPosition = Spinner.InvalidPosition;

        static string[] Commands = new string[] { CommandName.OnSelectionChange.Attribute };

        public AndroidPickerWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating picker element");
            Spinner picker = new Spinner(((AndroidControlWrapper)parent).Control.Context);
            this._control = picker;

            BindingContextPickerAdapter adapter = new BindingContextPickerAdapter(((AndroidControlWrapper)parent).Control.Context);
            picker.Adapter = adapter;

            applyFrameworkElementDefaults(picker);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                string itemContent = (string)bindingSpec["itemContent"] ?? "{$data}";

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getPickerContents(picker),
                    value => this.setPickerContents(picker, GetValueBinding("items").BindingContext, itemContent));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getPickerSelection(picker, selectionItem),
                    value => this.setPickerSelection(picker, selectionItem, (JToken)value));
            }

            picker.ItemSelected += picker_ItemSelected;
        }

        public JToken getPickerContents(Spinner picker)
        {
            logger.Debug("Getting picker contents - NOOP");
            throw new NotImplementedException();
        }

        public void setPickerContents(Spinner picker, BindingContext bindingContext, string itemContent)
        {
            logger.Debug("Setting picker contents");

            _selectionChangingProgramatically = true;

            BindingContextPickerAdapter adapter = (BindingContextPickerAdapter)picker.Adapter;
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
                this.setPickerSelection(picker, "$data", _localSelection);
            }

            _selectionChangingProgramatically = false;
        }

        public JToken getPickerSelection(Spinner picker, string selectionItem)
        {
            BindingContextPickerAdapter adapter = (BindingContextPickerAdapter)picker.Adapter;
            if (picker.SelectedItemPosition >= 0)
            {
                BindingContextListItem item = adapter.GetItemAtPosition(picker.SelectedItemPosition);
                return item.GetSelection(selectionItem);
            }
            return new MaaasCore.JValue(false); // This is a "null" selection
        }

        public void setPickerSelection(Spinner picker, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            BindingContextPickerAdapter adapter = (BindingContextPickerAdapter)picker.Adapter;
            for (int i = 0; i < adapter.Count; i++)
            {
                BindingContextListItem item = adapter.GetItemAtPosition(i);
                if (JToken.DeepEquals(selection, item.GetSelection(selectionItem)))
                {
                    _lastProgramaticallySelectedPosition = i;
                    picker.SetSelection(i);
                    break;
                }
            }

            _selectionChangingProgramatically = false;
        }

        async void picker_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            logger.Debug("Picker selection changed");
            Spinner picker = (Spinner)sender;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getPickerSelection(picker, "$data");
            }

            // ItemSelected gets called (at least) once during construction of the view.  In order to distinguish this
            // call from an actual user click we will test to see of the new selected item is different than the last 
            // one we set programatically.
            //
            if ((!_selectionChangingProgramatically) && (_lastProgramaticallySelectedPosition != e.Position))
            {
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    logger.Debug("Picker item click with command: {0}", command);

                    // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                    //
                    BindingContextPickerAdapter adapter = (BindingContextPickerAdapter)picker.Adapter;
                    BindingContextListItem listItem = adapter.GetItemAtPosition(e.Position);
                    await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(listItem.BindingContext));
                }
            }
        }
    }
}