using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MaaasClientWinPhone
{
    class WinPhoneAppManager : MaaasAppManager
    {
        private readonly static string STATE_KEY = "seed.json";
        private readonly static string UTF8_BYTE_ORDER_MARK = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble(), 0, Encoding.UTF8.GetPreamble().Length);

        private static async Task<string> ReadTextFile(string fileName, StorageFolder folder = null)
        {
            byte[] data;
            folder = folder ?? ApplicationData.Current.LocalFolder;

            try
            {
                var file = await folder.GetFileAsync(fileName);

                using (Stream s = await file.OpenStreamForReadAsync())
                {
                    data = new byte[s.Length];
                    await s.ReadAsync(data, 0, (int)s.Length);
                }

                // !!! Creepy.  Not sure if there's a cleaner way to read text files that would just properly handle
                //     the UTF-8 BOM.  For now, it blows up the JSON parse, so we prune it manually.
                //
                string stringData = Encoding.UTF8.GetString(data, 0, data.Length);
                stringData = stringData.TrimStart(UTF8_BYTE_ORDER_MARK.ToCharArray());

                return stringData;
            }
            catch (FileNotFoundException ex)
            {
                // File doesn't exist...
                //
                return null;
            }
        }

        private static async Task WriteTextFile(string fileName, string content, StorageFolder folder = null)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);

            folder = folder ?? ApplicationData.Current.LocalFolder; 
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
 
            using (Stream s = await file.OpenStreamForWriteAsync())
            {
                await s.WriteAsync(data, 0, data.Length);
            }
        }

        protected override async Task<string> loadBundledState()
        {
            return await ReadTextFile(@"Assets\seed.json", Windows.ApplicationModel.Package.Current.InstalledLocation);
        }

        protected async override Task<string> loadLocalState()
        {
            string state = await ReadTextFile(STATE_KEY);
            return state;
        }

        protected async override Task<bool> saveLocalState(string state)
        {
            await WriteTextFile(STATE_KEY, state);
            return true;
        }
    }
}
