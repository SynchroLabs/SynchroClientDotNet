using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace MaaasClientIOS
{
    public class iOSUtil
    {
        public static bool IsiOS7
        {
            get { return UIDevice.CurrentDevice.CheckSystemVersion(7, 0); }
        }
    }
}
