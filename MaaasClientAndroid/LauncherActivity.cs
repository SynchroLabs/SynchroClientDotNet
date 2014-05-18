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

    [Activity(Label = "MaaaS IO", MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class LauncherActivity : Activity
    {
        List<MaaasApp> tableItems = new List<MaaasApp>();
        ListView listView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.launcher);
            listView = FindViewById<ListView>(Resource.Id.appList);

            MaaasAppManager appManager = new StatelessAppManager();
            appManager.loadState();

            if (appManager.AppSeed != null)
            {
                // There was an AppSeed, so let's launch that now and not present the Launcher UX...
                //
                // !!! We need to prevent navigation "back" to the Launcher
                //
                MaaasPageActivity.MaaasApp = appManager.AppSeed;
                var intent = new Intent(this, typeof(MaaasPageActivity));
                StartActivity(intent);
            }
            else
            {
                // Fill list view with choices...
                //
                foreach (MaaasApp app in appManager.Apps)
                {
                    tableItems.Add(app);
                }

                listView.Adapter = new MaaasAppAdapter(this, tableItems);
                listView.ItemClick += OnListItemClick;
            }
        }

        protected void OnListItemClick(object sender, Android.Widget.AdapterView.ItemClickEventArgs e)
        {
            var listView = sender as ListView;
            MaaasApp maaasApp = tableItems[e.Position];

            MaaasPageActivity.MaaasApp = maaasApp;
            var intent = new Intent(this, typeof(MaaasPageActivity));
            StartActivity(intent);
        }
    }
}