using MonoTouch.CoreLocation;
using SynchroCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaaasClientIOS.Controls
{
    class iOSLocationWrapper : iOSControlWrapper
    {
        static Logger logger = Logger.GetLogger("iOSLocationWrapper");

        static string[] Commands = new string[] { CommandName.OnUpdate.Attribute };

        bool _updateOnChange = false;

        CLLocationManager _locMgr;

        LocationStatus _status = LocationStatus.Unknown;
        CLLocation _location;

        public iOSLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            _isVisualElement = false;

            int threshold = (int)ToDouble(controlSpec["movementThreshold"], 100);

            _locMgr = new CLLocationManager();

            _status = fromNativeStatus(CLLocationManager.Status);
            logger.Info("Native status: {0}, Synchro status: {1}", CLLocationManager.Status, _status);

            if (CLLocationManager.LocationServicesEnabled)
            {
                if (_locMgr.RespondsToSelector(new MonoTouch.ObjCRuntime.Selector("requestWhenInUseAuthorization")))
                {
                    // RequestWhenInUseAuthorization is only present in iOS 8.0 and later.  If available, we nee
                    // to call it to get authorized (using our custom message defined in Info.plist under the key: 
                    // NSLocationWhenInUseUsageDescription).  If not present, we don't call it (prior to iOS 8.0, 
                    // the operating system just pops up a generic permission dialog automatically when the below
                    // location services are accessed).
                    //
                    _locMgr.RequestWhenInUseAuthorization();
                }

                _locMgr.DesiredAccuracy = 100; //desired accuracy, in meters
                _locMgr.DistanceFilter = threshold;
                _locMgr.AuthorizationChanged += locMgr_AuthorizationChanged;
                _locMgr.LocationsUpdated += locMgr_LocationsUpdated;
                _locMgr.Failed += locMgr_Failed;
                _locMgr.StartUpdatingLocation();
                logger.Info("Status: {0} after", CLLocationManager.Status);
                logger.Info("Location services started");
            }
            else
            {
                logger.Info("Location services not enabled");
                _status = LocationStatus.NotAvailable;
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            processElementBoundValue("value", (string)bindingSpec["value"], () =>
            {
                JObject obj = new JObject()
                {
                    { "available", new JValue((_status == LocationStatus.Available) || (_status == LocationStatus.Active)) },
                    { "status", new JValue(_status.ToString()) }
                };

                if (_location != null)
                {
                    obj.Add("coordinate", new JObject()
                    {
                        { "latitude", new JValue(_location.Coordinate.Latitude) },
                        { "longitude", new JValue(_location.Coordinate.Longitude) }
                    });

                    obj.Add("accuracy", new JValue(_location.HorizontalAccuracy));

                    /*
                     * Altitude is kind of a train wreck on Windows and Android, so we are supressing it here
                     * also.  Docs claim it to be meters above/below sea level (could not confirm).
                     * 
                    if (_location.VerticalAccuracy >= 0)
                    {
                        obj.Add("altitude", new JValue(_location.Altitude));
                        obj.Add("altitudeAccuracy", new JValue(_location.VerticalAccuracy));
                    }
                     */

                    if (_location.Course >= 0)
                    {
                        obj.Add("heading", new JValue(_location.Course));
                    }

                    if (_location.Speed >= 0)
                    {
                        obj.Add("speed", new JValue(_location.Speed));
                    }

                    // _location.Timestamp // NSDate
                }

                return obj;
            });

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            // This triggers the viewModel update so the initial status gets back to the server
            //
            updateValueBindingForAttribute("value");
        }

        protected void stopLocationServices()
        {
            if (_locMgr != null)
            {
                _locMgr.StopUpdatingLocation();
                _locMgr.AuthorizationChanged -= locMgr_AuthorizationChanged;
                _locMgr.LocationsUpdated -= locMgr_LocationsUpdated;
                _locMgr.Failed -= locMgr_Failed;
            }
            _locMgr = null;
        }

        public override void Unregister()
        {
            stopLocationServices();
            base.Unregister();
        }

        async void locMgr_Failed(object sender, MonoTouch.Foundation.NSErrorEventArgs e)
        {
            _location = null;

            if (e.Error.Code == (int)CLError.LocationUnknown)
            {
                // "Location unknown" is not really an error.  It just indicates that the location couldn't be determined
                // immediately (it's going to keep trying), per...
                //
                // https://developer.apple.com/library/ios/documentation/CoreLocation/Reference/CLLocationManagerDelegate_Protocol/#//apple_ref/occ/intfm/CLLocationManagerDelegate/locationManager:didFailWithError:
                //
                logger.Info("Location manager could not immediately determine location, still trying");
                _status = LocationStatus.Available;
            }
            else
            {
                logger.Info("Location manager failed: {0}", e.Error);
                _status = LocationStatus.Failed;
            }

            // Update the viewModel, and the server (if update on change specified)
            //
            updateValueBindingForAttribute("value");
            if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }

        protected LocationStatus fromNativeStatus(CLAuthorizationStatus status)
        {
            if (status == CLAuthorizationStatus.Denied)
            {
                // The user explicitly denied the use of location services for this app or location
                // services are currently disabled in Settings.
                //
                return LocationStatus.NotApproved;
            }
            else if (status == CLAuthorizationStatus.Restricted)
            {
                // This app is not authorized to use location services. The user cannot change this app’s
                // status, possibly due to active restrictions such as parental controls being in place.
                //
                return LocationStatus.NotAvailable;
            }
            else if (status == CLAuthorizationStatus.NotDetermined)
            {
                // The user has not yet made a choice regarding whether this app can use location services.
                //
                return LocationStatus.PendingApproval;
            }
            else if ((status == CLAuthorizationStatus.AuthorizedAlways) || (status == CLAuthorizationStatus.AuthorizedWhenInUse))
            {
                return LocationStatus.Available;
            }

            return LocationStatus.Unknown;
        }

        async void locMgr_AuthorizationChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            logger.Info("Location manager authorization change: {0}", e.Status);
            _status = fromNativeStatus(e.Status);

            // Update the viewModel, and the server (if update on change specified)
            //
            updateValueBindingForAttribute("value");
            if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }

        async void locMgr_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            _status = LocationStatus.Active;
            _location = e.Locations[e.Locations.Length - 1];
            logger.Info("Location: {0}", _location);

            updateValueBindingForAttribute("value");

            CommandInstance command = GetCommand(CommandName.OnUpdate);
            if (command != null)
            {
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
            else if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }
    }
}
