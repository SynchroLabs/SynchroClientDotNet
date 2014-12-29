using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SynchroCore;

namespace MaaasClientIOS
{
    public class iOSDeviceMetrics : MaaasDeviceMetrics
    {
        private UIViewController _controller;

        static bool iPadMini()
        {
            // http://theiphonewiki.com/wiki/Models
            //
            string[] iPadMiniNames = 
            {
                "iPad2,5", // Mini
                "iPad2,6", // 
                "iPad2,7", //
                "iPad4,4", // Retina Mini
                "iPad4,5"  //
            };

            return iPadMiniNames.Contains(System.Environment.MachineName);
        }

        public iOSDeviceMetrics(UIViewController controller) : base()
        {
            _controller = controller;
            _os = "iOS";
            _osName = "iOS";

            // Device         Screen size  Logical resolution  Logical ppi  Width (in)  Height (in)
            // =============  ===========  ==================  ===========  ==========  ===========
            // iPhone / iPod     3.5"           320 x 480           163       1.963       2.944
            // iPhone / iPod     4.0"           320 x 568           163       1.963       3.485
            // iPad              9.7"           768 x 1024          132       5.818       7.758
            // iPad Mini         7.85"          768 x 1024          163       4.712       6.282

            // Screen size in inches is logical resolution divided by logical ppi
            // Physical ppi is logical ppi times scale

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                _deviceName = "iPhone/iPod";
                _deviceClass = MaaasDeviceClass.Phone;
                _naturalOrientation = MaaasOrientation.Portrait;

                _widthInches = 1.963f;
                if (UIScreen.MainScreen.Bounds.Height == 568)
                {
                    _heightInches = 3.485f;
                }
                else
                {
                    _heightInches = 2.944f;
                }
            }
            else if (iPadMini())
            {
                _deviceName = "iPad Mini";
                _deviceClass = MaaasDeviceClass.MiniTablet;
                _naturalOrientation = MaaasOrientation.Landscape;

                _widthInches = 6.282f;
                _heightInches = 4.712f;
            }
            else
            {
                _deviceName = "iPad";
                _deviceClass = MaaasDeviceClass.Tablet;
                _naturalOrientation = MaaasOrientation.Landscape;

                _widthInches = 7.758f;
                _heightInches = 5.818f;
            }

            if (_naturalOrientation == MaaasOrientation.Portrait)
            {
                // MainScreen.Bounds assumes portrait layout
                _widthDeviceUnits = UIScreen.MainScreen.Bounds.Width;
                _heightDeviceUnits = UIScreen.MainScreen.Bounds.Height;
            }
            else
            {
                _heightDeviceUnits = UIScreen.MainScreen.Bounds.Width;
                _widthDeviceUnits = UIScreen.MainScreen.Bounds.Height;
            }

            _deviceScalingFactor = UIScreen.MainScreen.Scale;

            this.updateScalingFactor();
        }

        public override MaaasOrientation CurrentOrientation
        {
            get
            {
                if ((_controller.InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft) ||
                    (_controller.InterfaceOrientation == UIInterfaceOrientation.LandscapeRight))
                {
                    return MaaasOrientation.Landscape;
                }
                else
                {
                    return MaaasOrientation.Portrait;
                }
            }
        }
    }
}