using Android.Content.PM;
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
        private MaaasPageActivity _activity;

        DisplayMetrics _metrics = new DisplayMetrics();

        public AndroidDeviceMetrics(MaaasPageActivity activity) : base()
        {
            _activity = activity;

            _os = "Android";
            _osName = "Android";
            _deviceName = "Android Device"; // !!! Actual device manufaturer/model would be nice

            // Galaxy S3 - DisplayMetrics{density=2.0, width=720, height=1280, scaledDensity=2.0, xdpi=304.799, ydpi=306.716}
            //             DensityDpi: Xhigh
            //
            Display display = _activity.WindowManager.DefaultDisplay;
            display.GetMetrics(_metrics);

            // !!! This could be a little more sophisticated - for now, largish is considered a "tablet", smaller is a "phone"
            //
            double screenDiagonalInches = Math.Sqrt(Math.Pow(_widthInches, 2) + Math.Pow(_heightInches, 2));
            if (screenDiagonalInches > 6.5f)
            {
                _deviceClass = MaaasDeviceClass.Tablet;
                _naturalOrientation = MaaasOrientation.Landscape;
            }
            else
            {
                _deviceClass = MaaasDeviceClass.Phone;
                _naturalOrientation = MaaasOrientation.Portrait;
            }

            if (CurrentOrientation == _naturalOrientation)
            {
                _widthInches = _metrics.WidthPixels / _metrics.Xdpi;
                _heightInches = _metrics.HeightPixels / _metrics.Ydpi;
                _widthDeviceUnits = _metrics.WidthPixels;
                _heightDeviceUnits = _metrics.HeightPixels;
            }
            else
            {
                _widthInches = _metrics.HeightPixels / _metrics.Xdpi;
                _heightInches = _metrics.WidthPixels / _metrics.Ydpi;
                _widthDeviceUnits = _metrics.HeightPixels;
                _heightDeviceUnits = _metrics.WidthPixels;
            }

            this.updateScalingFactor();
        }

        public override MaaasOrientation CurrentOrientation
        {
            get
            {
                ScreenOrientation orientation = _activity.GetScreenOrientation();

                if ((orientation == ScreenOrientation.Landscape) || (orientation == ScreenOrientation.ReverseLandscape))
                {
                    return MaaasOrientation.Landscape;
                }
                return MaaasOrientation.Portrait;
            }
        }
    }
}
