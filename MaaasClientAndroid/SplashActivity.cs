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

namespace SynchroClientAndroid
{
    [Activity(Label = "Synchro", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.Splash", NoHistory = true)]
    public class SplashActivity : Activity
    {
        async protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            MaaasAppManager appManager = new AndroidAppManager(this);
            await appManager.loadState();

            if (appManager.AppSeed != null)
            {
                // There was an AppSeed, so let's launch that now and not present the Launcher UX...
                //
                var intent = new Intent(this, typeof(MaaasPageActivity));
                intent.PutExtra("endpoint", appManager.AppSeed.Endpoint);
                StartActivity(intent);
            }
            else
            {
                // Go to launcher...
                //
                var intent = new Intent(this, typeof(LauncherActivity));
                StartActivity(intent);
            }
        }
    }
}