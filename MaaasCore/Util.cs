using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public static class Util
    {
        public static void debug(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }

        public static async Task GetStreamFromUrl(Uri url, Action<Stream> streamHandler)
        {
            using (var client = new HttpClient())
            {
                var msg = await client.GetAsync(url);
                if (msg.IsSuccessStatusCode)
                {
                    using (var stream = await msg.Content.ReadAsStreamAsync())
                    {
                        streamHandler(stream);
                    }
                }
            }
            return;
        }

        public static async Task<byte[]> GetResponseBytes(Uri uri)
        {
            var _httpClient = new HttpClient();
            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
