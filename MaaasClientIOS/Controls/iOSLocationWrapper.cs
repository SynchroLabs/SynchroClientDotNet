using MaaasCore;
using MonoTouch.CoreLocation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaaasClientIOS.Controls
{
    class iOSLocationWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSLocationWrapper");

        CLLocationManager _locMgr;

        public iOSLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            _isVisualElement = false;

            _locMgr = new CLLocationManager();

            logger.Info("Status: {0}", CLLocationManager.Status);
            logger.Info("Significant change monitoring available: {0}", CLLocationManager.SignificantLocationChangeMonitoringAvailable);
            if (CLLocationManager.LocationServicesEnabled)
            {
                _locMgr.RequestWhenInUseAuthorization();
                _locMgr.DesiredAccuracy = 10; //desired accuracy, in meters
                _locMgr.LocationsUpdated += locMgr_LocationsUpdated;
                _locMgr.Failed += _locMgr_Failed;
                _locMgr.StartUpdatingLocation();
                // _locMgr.DistanceFilter = 10; // number of meters of distance change required to trigger update
                logger.Info("Status: {0} after", CLLocationManager.Status);
                logger.Info("Location services started");
            }
            else
            {
                logger.Info("Location services not enabled");
            }
        }

        void _locMgr_Failed(object sender, MonoTouch.Foundation.NSErrorEventArgs e)
        {
            logger.Info("Location manager failed: {0}", e.Error);
        }

        void locMgr_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            logger.Info("Location: {0}", e.Locations[e.Locations.Length - 1]);
            _locMgr.StopUpdatingLocation();
        }
    }
}
