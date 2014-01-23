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
    // http://docs.xamarin.com/guides/android/user_interface/working_with_listviews_and_adapters/part_3_-_customizing_a_listview%27s_appearance/
    //
    // Source for resources
    //
    //     https://github.com/android/platform_frameworks_base/tree/master/core/res/res/layout
    //

    class AndroidListBoxWrapper : AndroidControlWrapper
    {
        public AndroidListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listbox element");

            ListView listView = new ListView(((AndroidControlWrapper)parent).Control.Context);
            this._control = listView;

            int listTemplate = Android.Resource.Layout.SimpleListItem1;

            // Get selection mode - None (default), Single, or Multiple - no dynamic values (we don't need this changing during execution).
            // !!! Other platforms (specifically Windows) don't have the concept of "None", and use "Single" as the default.  Resolve.
            if (controlSpec["select"] != null)
            {
                if ((string)controlSpec["select"] == "Single")
                {
                    listTemplate = Android.Resource.Layout.SimpleListItemSingleChoice;
                    listView.ChoiceMode = ChoiceMode.Single;
                }
                else if ((string)controlSpec["select"] == "Multiple")
                {
                    listTemplate = Android.Resource.Layout.SimpleListItemMultipleChoice;
                    listView.ChoiceMode = ChoiceMode.Multiple;
                }
            }

            String[] values = new String[] { };
            ArrayAdapter adapter = new ArrayAdapter(((AndroidControlWrapper)parent).Control.Context, listTemplate, values);
            listView.Adapter = adapter;
            
            setListViewHeightBasedOnChildren();

            applyFrameworkElementDefaults(listView);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items");
            if (bindingSpec != null)
            {
                if (bindingSpec["items"] != null)
                {
                    processElementBoundValue("items", (string)bindingSpec["items"], () => getListboxContents(listView), value => this.setListboxContents(listView, (JToken)value));
                }
                if (bindingSpec["selection"] != null)
                {
                    processElementBoundValue("selection", (string)bindingSpec["selection"], () => getListboxSelection(listView), value => this.setListboxSelection(listView, (JToken)value));
                }
            }

            listView.ItemSelected += listView_ItemSelected;
            listView.NothingSelected += listView_NothingSelected;
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

        void listView_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Util.debug("ListBox item selected at position: " + e.Position);
            this.listbox_SelectionChanged();
        }

        void listView_NothingSelected(object sender, AdapterView.NothingSelectedEventArgs e)
        {
            Util.debug("ListBox nothing selected");
            this.listbox_SelectionChanged();
        }

        void listView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            CheckedTextView itemView = (CheckedTextView)e.View;
            Util.debug("ListBox item clicked, text: " + itemView.Text + ", checked: " + itemView.Checked);
            this.listbox_SelectionChanged();
        }

        public JToken getListboxContents(ListView listView)
        {
            Util.debug("Getting listbox contents");

            List<string> items = new List<string>();
            for (int n = 0; n < listView.Count; n++)
            {
                items.Add(listView.GetItemAtPosition(n).ToString());
            }

            return new JArray(
                from item in items
                select new Newtonsoft.Json.Linq.JValue(item)
                );

        }

        public void setListboxContents(ListView listView, JToken contents)
        {
            Util.debug("Setting listbox contents");

            ArrayAdapter adapter = (ArrayAdapter)listView.Adapter;

            // Keep track of currently selected item/items so we can restore after repopulating list...
            //
            List<string> selected = new List<string>();
            var checkedItems = listView.CheckedItemPositions;
            for (var i = 0; i < checkedItems.Size(); i++)
            {
                int key = checkedItems.KeyAt(i);
                if (checkedItems.Get(key))
                {
                    selected.Add(listView.GetItemAtPosition(key).ToString());
                }
            }
            checkedItems.Clear();

            // Clear the list and refill...
            //
            adapter.Clear(); 
            if ((contents != null) && (contents.Type == JTokenType.Array))
            {
                // !!! Default itemValue is "$data"
                foreach (JToken arrayElementBindingContext in (JArray)contents)
                {
                    // !!! If $data (default), then we get the value of the binding context iteration items.
                    //     Otherwise, if there is a non-default itemData binding, we apply that.
                    string value = (string)arrayElementBindingContext;
                    Util.debug("adding listbox item: " + value);
                    adapter.Add(value);
                }

                // Reselect items...
                //
                for (int n = 0; n < listView.Count; n++)
                {
                    string listItem = listView.GetItemAtPosition(n).ToString();
                    if (selected.Contains(listItem))
                    {
                        checkedItems.Put(n, true);
                    }
                }

                adapter.NotifyDataSetChanged();
                setListViewHeightBasedOnChildren(); // !!!?
            }
        }

        public JToken getListboxSelection(ListView listView)
        {
            if (listView.ChoiceMode == ChoiceMode.Multiple)
            {
                List<string> selected = new List<string>();
                var checkedItems = listView.CheckedItemPositions;
                for (var i = 0; i < checkedItems.Size(); i++)
                {
                    int key = checkedItems.KeyAt(i);
                    if (checkedItems.Get(key))
                    {
                        selected.Add(listView.GetItemAtPosition(key).ToString());
                    }
                }

                return new JArray(
                    from selection in selected
                    select new Newtonsoft.Json.Linq.JValue(selection)
                    );
            }
            else if (listView.ChoiceMode == ChoiceMode.Single)
            {
                return new Newtonsoft.Json.Linq.JValue(listView.GetItemAtPosition(listView.CheckedItemPosition).ToString());
            }

            return null;
        }

        public void setListboxSelection(ListView listView, JToken selection)
        {
            List<string> selected = new List<string>();
            if (selection is JArray)
            {
                JArray array = selection as JArray;
                foreach (JToken item in array.Values())
                {
                    selected.Add(ToString(item));
                }
            }
            else
            {
                selected.Add(ToString(selection));
            }

            var checkedItems = listView.CheckedItemPositions;
            checkedItems.Clear();
            for (int n = 0; n < listView.Count; n++)
            {
                string listItem = listView.GetItemAtPosition(n).ToString();
                if (selected.Contains(listItem))
                {
                    checkedItems.Put(n, true);
                }
            }

            ArrayAdapter adapter = (ArrayAdapter)listView.Adapter;
            adapter.NotifyDataSetChanged();
        }

        void listbox_SelectionChanged()
        {
            updateValueBindingForAttribute("selection");
        }
    }
}