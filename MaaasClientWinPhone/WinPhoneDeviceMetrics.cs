using MaaasCore;
using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Display;

namespace MaaasClientWinPhone
{
    class WinPhoneDeviceMetrics : MaaasDeviceMetrics
    {
        // Windows Phone prior to GDR3 had no way to access the physical size of the screen.  All screens were scaled from a
        // virtual 480 x 800/853 resolution.
        //
        // Windows Phone 8 GDR 3 added some metrics to give the actual screen size / ppi so that size-aware apps could take 
        // advantage of potentially much larger screens.  Those devices can be as large as 6" - 7".
        //
        //   http://blogs.windows.com/windows_phone/b/wpdev/archive/2013/11/22/taking-advantage-of-large-screen-windows-phones.aspx
        //
        // Note that the GDR3 emulators return a value for PhysicalScreenResolution, but not for RawDpiX.  Nokia's suggested 
        // workaround for testing is here:
        //
        //   http://developer.nokia.com/Community/Wiki/Simulate_1080p_windows_phone_emulator
        //
        // This code was tested on a Nokia 920 with GDR3, which provided both PhysicalScreenResolution and RawDpiX, and using
        // those values, produced correct values for screen width/height in inches.
        //
        bool GetExtendedScreenInfo()
        {
            object temp;

            if (!DeviceExtendedProperties.TryGetValue("PhysicalScreenResolution", out temp))
            {
                Util.debug("Extended screen info not available");
                return false;
            }

            var screenResolution = (Size)temp;

            // Can query for RawDpiY as well, but it will be the same value
            if (!DeviceExtendedProperties.TryGetValue("RawDpiX", out temp) || (double)temp == 0d)
            {
                Util.debug("Extended screen info not available");
                return false;
            }

            var dpi = (double)temp;

            this._widthInches = screenResolution.Width / dpi;
            this._heightInches = screenResolution.Height / dpi;

            // It might be nice to record this, as for very large scale values Windows Phone will report
            // a lower / more common scaling factor (for example, on a Nokia 1520 the actual scale factor
            // is 2.25, but the OS will report 1.6, as if it was a 720p screen instead of 1080p).  Using
            // the reported scaling factor for selecting resources will work fine (they'll be scaled up
            // by the OS as needed), but if the scaling factor is used for anything else, you might want
            // the real deal.
            //
            double rawScalingFactor = screenResolution.Width / this._widthDeviceUnits;

            Util.debug(String.Format("Extended screen info resolution {0} x {1}; {2:0.0#} raw scale; {3:0.0}\" x {4:0.0}\"",
                screenResolution.Width, screenResolution.Height, rawScalingFactor, this._widthInches, this._heightInches));

            return true;
        }

        public WinPhoneDeviceMetrics() : base()
        {
            _os = "WinPhone";
            _osName = "Windows Phone";
            _deviceName = "Windows Phone Device"; // !!! Actual device manufaturer/model would be nice

            _deviceClass = MaaasDeviceClass.Phone;

            _widthDeviceUnits = Application.Current.Host.Content.ActualWidth;
            _heightDeviceUnits = Application.Current.Host.Content.ActualHeight;

            _deviceScalingFactor = Application.Current.Host.Content.ScaleFactor / 100;

            // We check extended screen info, and if present, use that (as it will give us accurate information)
            //
            if (!GetExtendedScreenInfo())
            {
                // If extended screen info fails (pre GDR3), then we will use the guestimate method below...
                //
                // Windows phones at lower resolutions (unscaled) are typically 4" to 4.3", so we'll assume
                // 4.25" for those phones.  Windows phones with higher resolution displays tend to also be
                // larger, typically 4.3" to 4.8", so we'll assume 4.5" for those.
                //
                double screenDiagonalInches = 4.25f;
                if (_deviceScalingFactor > 1.0f)
                {
                    screenDiagonalInches = 4.5f;
                }

                double screenDiagonalDeviceUnits = Math.Sqrt(Math.Pow(_widthDeviceUnits, 2) + Math.Pow(_heightDeviceUnits, 2));

                _widthInches = screenDiagonalInches / screenDiagonalDeviceUnits * _widthDeviceUnits;
                _heightInches = screenDiagonalInches / screenDiagonalDeviceUnits * _heightDeviceUnits;
            }

            this.updateScalingFactor();
        }
    }
}
