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
        List<String> _rowItems;

        public ListViewAdapter(Context context, List<String> items)
            : base()
        {
            _context = context;
            _rowItems = items;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView textView = new ListItemView(_context);
            textView.Text = _rowItems[position];
            return textView;
        }

        public override int Count { get { return _rowItems.Count; } }

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

            // Get selection mode - None (default), Single, or Multiple - no dynamic values (we don't need this changing during execution).
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

            String[] values = new String[] { "Android", "iPhone", "Windows", "WindowsPhone" };
            List<String> strings = new List<String>(values);
            ListViewAdapter adapter = new ListViewAdapter(((AndroidControlWrapper)parent).Control.Context, strings);
            listView.Adapter = adapter;

            setListViewHeightBasedOnChildren();

            // adapter.NotifyDataSetChanged();
            // listView.SetItemChecked(position, true); // Selection?
            applyFrameworkElementDefaults(listView);

            listView.ItemSelected += listView_ItemSelected;
            listView.NothingSelected += listView_NothingSelected;
            listView.ItemClick += listView_ItemClick;
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
        }

        void listView_NothingSelected(object sender, AdapterView.NothingSelectedEventArgs e)
        {
            Util.debug("ListView nothing selected");
        }

        void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            CheckedTextView itemView = (CheckedTextView)e.View;
            Util.debug("ListView item clicked, text: " + itemView.Text + ", checked: " + itemView.Checked);
        }
    }
}