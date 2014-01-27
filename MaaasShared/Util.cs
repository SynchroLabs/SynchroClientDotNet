using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public static class Util
    {
        public static string getMaaasHost()
        {
            // string host = "maaas.io:1337"; // "maaas.azurewebsites.net";
            string host = "192.168.1.144:1337"; // "localhost:1337";
            return host;
        }

        public static void debug(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
}
