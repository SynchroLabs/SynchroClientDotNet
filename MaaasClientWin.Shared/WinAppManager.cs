using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MaaasClientWin
{
    class WinAppManager : MaaasAppManager
    {
        private readonly static string STATE_KEY = "seed.json";

        Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private async Task<string> ReadTextFile(string filepath, StorageFolder folder = null)
        {
            folder = folder ?? ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync(filepath);
            return await Windows.Storage.FileIO.ReadTextAsync(file);
        }

        protected override async Task<string> loadBundledState()
        {
            return await ReadTextFile(@"Assets\seed.json", Windows.ApplicationModel.Package.Current.InstalledLocation);
        }

        protected override Task<string> loadLocalState()
        {
            string state = (string)_localSettings.Values[STATE_KEY];
            return Task.FromResult(state);
        }

        protected override Task<bool> saveLocalState(string state)
        {
            _localSettings.Values[STATE_KEY] = state;
            return Task.FromResult(true);
        }
    }
}
