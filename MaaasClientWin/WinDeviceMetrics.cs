using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace MaaasClientWin
{
    class WinDeviceMetrics : MaaasDeviceMetrics
    {
        // http://blogs.msdn.com/b/b8/archive/2012/03/21/scaling-to-different-screens.aspx

        public WinDeviceMetrics() : base()
        {
            _os = "Windows";
            _osName = "Windows";
            _deviceName = "Windows Device";

            _deviceClass = MaaasDeviceClass.Tablet;

            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

            // Surface Pro (sample values)
            //
            // Display properties - Orientation: Landscape
            // Display properties - RawDpiX: 207
            // Display properties - RawDpiY: 207
            // Display properties - LogicalDpi: 134.4 (default logical DPI of 96 times scaling factor of 1.4 = 134.4)
            // Display properties - ResolutionScale: Scale140Percent
            //
            // Windows.UI.Xaml.Window.Current.Bounds = 0, 0, 1371.429, 771.4286
            //
            //    Multiple bounds by scaling factor to get physical pixel size: 1920, 1080
            //
            //    Divide physical pixel size by raw DPI to get physical screen size:
            //
            //       1920 / 207 = 9.275"
            //       1080 / 207 = 5.217"
            //

            // !!! Note that this is the "current" window size, and not necessarily the screen size (in non-full screen
            //     views), and that these values can change as the view/mode changes...
            //
            _widthDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Width;
            _heightDeviceUnits = Windows.UI.Xaml.Window.Current.Bounds.Height;

            _deviceScalingFactor = 1.0d;
            switch (displayInfo.ResolutionScale)
            {
                case ResolutionScale.Scale100Percent:
                    break;

                case ResolutionScale.Scale120Percent:
                    _deviceScalingFactor = 1.2d;
                    break;

                case ResolutionScale.Scale140Percent:
                    _deviceScalingFactor = 1.4d;
                    break;

                case ResolutionScale.Scale150Percent:
                    _deviceScalingFactor = 1.5d;
                    break;

                case ResolutionScale.Scale160Percent:
                    _deviceScalingFactor = 1.6d;
                    break;

                case ResolutionScale.Scale180Percent:
                    _deviceScalingFactor = 1.8d;
                    break;

                case ResolutionScale.Scale225Percent:
                    _deviceScalingFactor = 2.25d;
                    break;
            }

            _widthInches = _widthDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiX;
            _heightInches = _heightDeviceUnits * _deviceScalingFactor / displayInfo.RawDpiY;

            this.updateScalingFactor();
        }
    }
}
