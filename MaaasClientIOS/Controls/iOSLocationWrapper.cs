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

        bool _updateOnChange = false;

        CLLocationManager _locMgr;
        CLLocation _location;

        public iOSLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            _isVisualElement = false;

            int threshold = (int)ToDouble(controlSpec["movementThreshold"], 100);

            _locMgr = new CLLocationManager();

            logger.Info("Status: {0}", CLLocationManager.Status);
            logger.Info("Significant change monitoring available: {0}", CLLocationManager.SignificantLocationChangeMonitoringAvailable);
            if (CLLocationManager.LocationServicesEnabled)
            {
                _locMgr.RequestWhenInUseAuthorization();
                _locMgr.DesiredAccuracy = 100; //desired accuracy, in meters
                _locMgr.DistanceFilter = threshold;
                _locMgr.LocationsUpdated += locMgr_LocationsUpdated;
                _locMgr.Failed += _locMgr_Failed;
                _locMgr.StartUpdatingLocation();
                logger.Info("Status: {0} after", CLLocationManager.Status);
                logger.Info("Location services started");
            }
            else
            {
                logger.Info("Location services not enabled");
            }
            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            processElementBoundValue("value", (string)bindingSpec["value"], () =>
            {
                JObject obj =  new JObject(
                    new JProperty("latitude", _location.Coordinate.Latitude),
                    new JProperty("longitude", _location.Coordinate.Longitude),
                    new JProperty("accuracy", _location.HorizontalAccuracy)
                );

                /*
                 * Altitude is kind of a train wreck on Windows and Android, so we are supressing it here
                 * also.  Docs claim it to be meters above/below sea level (could not confirm).
                 * 
                if (_location.VerticalAccuracy >= 0)
                {
                    obj.Add(new JProperty("altitude", _location.Altitude));
                    obj.Add(new JProperty("altitudeAccuracy", _location.VerticalAccuracy));
                }
                 */

                if (_location.Course >= 0)
                {
                    obj.Add(new JProperty("heading", _location.Course));
                }

                if (_location.Speed >= 0)
                {
                    obj.Add(new JProperty("speed", _location.Speed));
                }

                // _location.Timestamp // NSDate

                return obj;
            });

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }
        }

        public override void Unregister()
        {
            _locMgr.StopUpdatingLocation();
            base.Unregister();
        }

        void _locMgr_Failed(object sender, MonoTouch.Foundation.NSErrorEventArgs e)
        {
            logger.Info("Location manager failed: {0}", e.Error);
        }

        async void locMgr_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            _location = e.Locations[e.Locations.Length - 1];
            logger.Info("Location: {0}", _location);

            updateValueBindingForAttribute("value");
            if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }
    }
}
