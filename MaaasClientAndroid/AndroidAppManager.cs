using Android.App;
using Android.Content;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroClientAndroid
{
    class AndroidAppManager : MaaasAppManager
    {
        private readonly static string STATE_FILE = "synchro";
        private readonly static string STATE_KEY = "seed.json";

        protected Activity _activity;

        public AndroidAppManager(Activity activity) : base()
        {
            _activity = activity;
        }

        protected override Task<string> loadBundledState()
        {
            string contents;
            using (StreamReader sr = new StreamReader(_activity.Assets.Open("seed.json")))
            {
                contents = sr.ReadToEnd();
            }
            return Task.FromResult(contents);
        }

        protected override Task<string> loadLocalState()
        {
            ISharedPreferences preferences = _activity.GetSharedPreferences(STATE_FILE, FileCreationMode.Private);
            //ISharedPreferences preferences = _activity.GetPreferences(FileCreationMode.Private);
            string state = preferences.GetString(STATE_KEY, null);
            return Task.FromResult(state);
        }

        protected override Task<bool> saveLocalState(string state)
        {
            ISharedPreferences preferences = _activity.GetSharedPreferences(STATE_FILE, FileCreationMode.Private);
            // ISharedPreferences preferences = _activity.GetPreferences(FileCreationMode.Private);
            ISharedPreferencesEditor editor = preferences.Edit();
            editor.PutString(STATE_KEY, state);
            editor.Commit();
            return Task.FromResult(true);
        }    
    }
}
