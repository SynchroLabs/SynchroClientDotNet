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
using Android.Graphics.Drawables;

namespace MaaasClientAndroid
{
    public class MaaasAppAdapter : BaseAdapter<MaaasApp> 
    {
        List<MaaasApp> items;
        Activity context;

        public MaaasAppAdapter(Activity context, List<MaaasApp> items) : base()
        {
            this.context = context;
            this.items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override MaaasApp this[int position]
        {   
            get { return items[position]; } 
        }

        public override int Count 
        {
            get { return items.Count; } 
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];

            View view = convertView;
            if (view == null)
            {
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);
            }

            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = item.Name + " - " + item.Description;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = item.Endpoint;
            
            return view;
        }
    }

    [Activity(Label = "Synchro", Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class LauncherActivity : Activity
    {
        List<MaaasApp> tableItems = new List<MaaasApp>();
        ListView listView;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.launcher);
            listView = FindViewById<ListView>(Resource.Id.appList);

            MaaasAppManager appManager = new AndroidAppManager(this);
            await appManager.loadState();

            // Fill list view with choices...
            //
            foreach (MaaasApp app in appManager.Apps)
            {
                tableItems.Add(app);
            }

            listView.Adapter = new MaaasAppAdapter(this, tableItems);
            listView.ItemClick += OnListItemClick;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            IMenuItem menuItem = menu.Add(0, 0, 0, "Add");

            int iconResourceId = (int)typeof(Resource.Drawable).GetField("ic_action_new").GetValue(null);
            if (iconResourceId > 0)
            {
                Drawable icon = this.Resources.GetDrawable(iconResourceId);
                menuItem.SetIcon(icon);
            }

            // Show it on the action bar...
            menuItem.SetShowAsAction(ShowAsAction.Always);

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == 0)
            {
                var intent = new Intent(this, typeof(AppDetailActivity));

                // If we don't put the null endpoint value, there is quite a Xamarin freakout *after* the OnCreate in the
                // target activity completes, if the target activity tries to access Extras in any way (like to see if an
                // endpoint was provided).
                //
                intent.PutExtra("endpoint", (string)null);

                StartActivity(intent);
            }

            return true;
        }

        protected void OnListItemClick(object sender, Android.Widget.AdapterView.ItemClickEventArgs e)
        {
            var listView = sender as ListView;
            MaaasApp maaasApp = tableItems[e.Position];

            //var intent = new Intent(this, typeof(MaaasPageActivity));
            var intent = new Intent(this, typeof(AppDetailActivity));
            intent.PutExtra("endpoint", maaasApp.Endpoint);
            StartActivity(intent);
        }
    }
}