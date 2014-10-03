using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using MaaasCore;
using MaaasShared;
using Newtonsoft.Json.Linq;

namespace SynchroClientAndroid
{
    [Activity(Label = "Application", Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo")]
    public class AppDetailActivity : Activity
    {
        MaaasAppManager appManager;
        MaaasApp app;

        LinearLayout layoutFind;
        EditText editEndpoint;
        Button btnFind;

        LinearLayout layoutDetails ;
        TextView textEndpoint;
        TextView textName;
        TextView textDescription;
        Button btnSave;
        Button btnLaunch;
        Button btnDelete;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.appdetail);

            this.ActionBar.SetDisplayHomeAsUpEnabled(true);
          
            layoutFind = FindViewById<LinearLayout>(Resource.Id.linearLayoutFind);
            editEndpoint = FindViewById<EditText>(Resource.Id.editEndpoint);
            btnFind = FindViewById<Button>(Resource.Id.btnFind);

            btnFind.Click += btnFind_Click;

            layoutDetails = FindViewById<LinearLayout>(Resource.Id.linearLayoutDetails);
            textEndpoint = FindViewById<TextView>(Resource.Id.textEndpoint);
            textName = FindViewById<TextView>(Resource.Id.textName);
            textDescription = FindViewById<TextView>(Resource.Id.textDescription);
            btnSave = FindViewById<Button>(Resource.Id.btnSave);
            btnLaunch = FindViewById<Button>(Resource.Id.btnLaunch);
            btnDelete = FindViewById<Button>(Resource.Id.btnDelete);

            btnSave.Click += btnSave_Click;
            btnLaunch.Click += btnLaunch_Click;
            btnDelete.Click += btnDelete_Click;

            appManager = new AndroidAppManager(this);
            await appManager.loadState();
            
            string endpoint = this.Intent.Extras.GetString("endpoint", null);
            if (endpoint != null)
            {
                // App details mode...
                this.app = appManager.GetApp(endpoint);
                this.layoutFind.Visibility = ViewStates.Gone;
                this.layoutDetails.Visibility = ViewStates.Visible;
                this.btnSave.Visibility = ViewStates.Gone;
                this.btnLaunch.Visibility = ViewStates.Visible;
                this.btnDelete.Visibility = ViewStates.Visible;
                this.populateControlsFromApp();
            }
            else
            {
                // "Find" mode...
                this.layoutFind.Visibility = ViewStates.Visible;
                this.layoutDetails.Visibility = ViewStates.Gone;
            }
        }

        void populateControlsFromApp()
        {
            this.textEndpoint.Text = this.app.Endpoint;
            this.textName.Text = this.app.Name;
            this.textDescription.Text = this.app.Description;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                var intent = new Intent(this, typeof(LauncherActivity));
                NavUtils.NavigateUpTo(this, intent);
            }

            return true;
        }

        async void btnFind_Click(object sender, EventArgs e)
        {
            string endpoint = this.editEndpoint.Text;

            var managedApp = appManager.GetApp(endpoint);
            if (managedApp != null)
            {
                AlertDialog.Builder builder;
                builder = new AlertDialog.Builder(this);
                builder.SetTitle("Synchro Application Search");
                builder.SetMessage("You already have a Synchro application with the supplied endpoint in your list");
                builder.SetPositiveButton("OK", delegate {});
                builder.SetCancelable(true);
                builder.Show();
                return;
            }

            Transport transport = new TransportHttp(endpoint);

            JObject appDefinition = await transport.getAppDefinition();
            if (appDefinition == null)
            {
                AlertDialog.Builder builder;
                builder = new AlertDialog.Builder(this);
                builder.SetTitle("Synchro Application Search");
                builder.SetMessage("No Synchro application found at the supplied endpoint");
                builder.SetPositiveButton("OK", delegate { });
                builder.SetCancelable(true);
                builder.Show();
            }
            else
            {
                this.app = new MaaasApp(endpoint, appDefinition);
                this.layoutFind.Visibility = ViewStates.Gone;
                this.layoutDetails.Visibility = ViewStates.Visible;
                this.btnSave.Visibility = ViewStates.Visible;
                this.btnLaunch.Visibility = ViewStates.Gone;
                this.btnDelete.Visibility = ViewStates.Gone;
                this.populateControlsFromApp();
            }
        }

        async void btnSave_Click(object sender, EventArgs e)
        {
            appManager.Apps.Add(this.app);
            await appManager.saveState();
            var intent = new Intent(this, typeof(LauncherActivity));
            NavUtils.NavigateUpTo(this, intent);
        }

        void btnLaunch_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MaaasPageActivity));
            intent.PutExtra("endpoint", this.app.Endpoint);
            StartActivity(intent);
        }

        void btnDelete_Click(object sender, EventArgs e)
        {
            AlertDialog.Builder builder;
            builder = new AlertDialog.Builder(this);
            builder.SetTitle("Synchro Application Delete");
            builder.SetMessage("Are you sure you want to remove this Synchro application from your list");
            builder.SetPositiveButton("Yes", async delegate {
                MaaasApp app = appManager.GetApp(this.app.Endpoint);
                appManager.Apps.Remove(app);
                await appManager.saveState();
                var intent = new Intent(this, typeof(LauncherActivity));
                NavUtils.NavigateUpTo(this, intent);
            });
            builder.SetNegativeButton("No", delegate { });
            builder.SetCancelable(false);
            builder.Show();
        }
    }
}