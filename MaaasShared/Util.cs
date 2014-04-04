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
            //string host = "maaas.cloudapp.net";
            string host = "maaas.io"; 
            //string host = "192.168.1.105:1337"; // "localhost:1337";
            //string host = "localhost:1337";
            //string host = "127.0.0.1";
            return host;
        }

        public static void debug(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
}
