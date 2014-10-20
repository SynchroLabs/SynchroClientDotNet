using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace MaaasClientWin
{
    class WinPhoneDeviceMetrics : MaaasDeviceMetrics
    {
        static Logger logger = Logger.GetLogger("WinPhoneDeviceMetrics");

        public WinPhoneDeviceMetrics() : base()
        {
            _os = "WinPhone";
            _osName = "Windows Phone";
            _deviceName = "Windows Phone Device"; // !!! Actual device manufaturer/model would be nice

            _deviceClass = MaaasDeviceClass.Phone;
            _naturalOrientation = MaaasOrientation.Portrait;

            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

            _deviceScalingFactor = displayInfo.RawPixelsPerViewPixel;

            // !!! Note that this is the "current" window size, and not necessarily the screen size (in non-full screen
            //     views), and that these values can change as the view/mode changes...
            //
            if (CurrentOrientation == NaturalOrientation)
            {
                _widthDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Width;
                _heightDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Height;
                _widthInches = _widthDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiX;
                _heightInches = _heightDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiY;
            }
            else
            {
                _widthDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Height;
                _heightDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Width;
                _widthInches = _widthDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiY;
                _heightInches = _heightDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiX;
            }

            this.updateScalingFactor();
        }

        public override MaaasOrientation CurrentOrientation
        {
            get
            {
                ApplicationViewOrientation winOrientation = ApplicationView.GetForCurrentView().Orientation;
                if (winOrientation == ApplicationViewOrientation.Portrait)
                {
                    return MaaasOrientation.Portrait;
                }
                return MaaasOrientation.Landscape;
            }
        }
    }
}
