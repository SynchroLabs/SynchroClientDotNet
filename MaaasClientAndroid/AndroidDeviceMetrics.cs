using Android.Util;
using Android.Views;
using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaaasClientAndroid
{
    public class AndroidDeviceMetrics : MaaasDeviceMetrics
    {
        DisplayMetrics _metrics = new DisplayMetrics();

        public AndroidDeviceMetrics(Display display) : base()
        {
            // Galaxy S3 - DisplayMetrics{density=2.0, width=720, height=1280, scaledDensity=2.0, xdpi=304.799, ydpi=306.716}
            //             DensityDpi: Xhigh
            //
            display.GetMetrics(_metrics);

            _widthInches = _metrics.WidthPixels / _metrics.Xdpi;
            _heightInches = _metrics.HeightPixels / _metrics.Ydpi;
            _widthDeviceUnits = _metrics.WidthPixels;
            _heightDeviceUnits = _metrics.HeightPixels;

            _deviceType = MaaasDeviceType.Phone;
        }
    }
}
