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
        // OK, so Windows Phone prior to GDR3 had no way to access the physical size of the screen.  All screens were scaled from a
        // virtual 480 x 800/853 resolution.  Original Windows Phone 7 era phones were between 3.7" - 4.3", whereas more modern Windows
        // Phone 8 devices were typically in the 4"-4.5" range.
        //
        // Windows Phone 8 GDR 3 added some metrics to give the actual screen size / ppi so that size-aware apps could take advantage of
        // potentially much larger screens.  Those devices can be as large as 6" - 7".
        //
        // The general rule will be to try to get the physical size from the GDR extended screen info.  If that is not present, just 
        // make an assumption about the screen size (we know it's a pre-phablet phone).  It might be good to assume a smaller physical
        // screen for the lower resolution, such as 4" for a 100% scale device and 4.25" for devices with a larger scale factor.
        //
        // http://blogs.windows.com/windows_phone/b/wpdev/archive/2013/11/22/taking-advantage-of-large-screen-windows-phones.aspx
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

            var screenDiagonal = Math.Sqrt(Math.Pow(screenResolution.Width / dpi, 2) + Math.Pow(screenResolution.Height / dpi, 2));

            var width = App.Current.Host.Content.ActualWidth;

            Util.debug(String.Format("Extended screen info {0} x {1}; {2:0.0#} raw scale; {3:0.0}\"",
              screenResolution.Width, screenResolution.Height, screenResolution.Width / width,
              screenDiagonal));

            return true;
        }

        public WinPhoneDeviceMetrics() : base()
        {
            _deviceType = MaaasDeviceType.Phone;

            _widthDeviceUnits = Application.Current.Host.Content.ActualWidth;
            _heightDeviceUnits = Application.Current.Host.Content.ActualHeight;

            _scalingFactor = Application.Current.Host.Content.ScaleFactor;

            // !!! We need to check extended screen info, and if present, use that (as it will give us accurate information)
            //
            Util.debug("extended screen info:" + GetExtendedScreenInfo());

            // !!! Only if extended screen info fails (pre GDR3), then we will use the guestimate method below...
            //
            // Windows phones at lower resolutions (unscaled) are typically 4" to 4.3", so we'll assume
            // 4.25" for those phones.  Windows phones with higher resolution displays tend to also be
            // larger, typically 4.3" to 4.8", so we'll assume 4.5" for those.
            //
            double screenDiagonalInches = 4.25f;
            if (_scalingFactor > 1.0f)
            {
                screenDiagonalInches = 4.5f;
            }

            double screenDiagonalDeviceUnits = Math.Sqrt(Math.Pow(_widthDeviceUnits, 2) + Math.Pow(_heightDeviceUnits, 2));

            _widthInches = screenDiagonalInches / screenDiagonalDeviceUnits * _widthDeviceUnits;
            _heightInches = screenDiagonalInches / screenDiagonalDeviceUnits * _heightDeviceUnits;
        }
    }
}
