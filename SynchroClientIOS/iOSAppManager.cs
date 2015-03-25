using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SynchroCore;
using System.Threading.Tasks;
using System.IO;

namespace MaaasClientIOS
{
    class iOSAppManager : MaaasAppManager
    {
        private readonly static string STATE_KEY = "seed.json";

        protected override Task<string> loadBundledState()
        {
            string path = System.IO.Path.Combine(NSBundle.MainBundle.BundlePath, "seed.json");
            string contents = File.ReadAllText(path);
            return Task.FromResult(contents);
        }

        protected override Task<string> loadLocalState()
        {
            string state = NSUserDefaults.StandardUserDefaults.StringForKey(STATE_KEY);
            return Task.FromResult(state);
        }

        protected override Task<bool> saveLocalState(string state)
        {
            NSUserDefaults.StandardUserDefaults.SetString(state, STATE_KEY);
            return Task.FromResult(true);
        }
    }
}