using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Locations;
using SynchroCore;
using JValue = SynchroCore.JValue;

namespace SynchroClientAndroid.Controls
{
    class LocationListener : Java.Lang.Object, ILocationListener
    {
        static Logger logger = Logger.GetLogger("LocationListener");

        AndroidLocationWrapper _locationWrapper;

        public LocationListener(AndroidLocationWrapper locationWrapper) : base()
        {
            _locationWrapper = locationWrapper;
        }

        public void OnProviderEnabled(string provider)
        {
            _locationWrapper.OnProviderEnabled(provider);
        }

        public void OnProviderDisabled(string provider)
        {
            _locationWrapper.OnProviderDisabled(provider);
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            _locationWrapper.OnStatusChanged(provider, status, extras);
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            _locationWrapper.OnLocationChanged(location);
        }
    }

    class AndroidLocationWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidLocationWrapper");

        static string[] Commands = new string[] { CommandName.OnUpdate.Attribute };

        bool _updateOnChange = false;

        LocationManager _locMgr;
        LocationListener _listener;

        LocationStatus _status = LocationStatus.Unknown;
        Location _location;

        public AndroidLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            this._isVisualElement = false;

            int threshold = (int)ToDouble(controlSpec["movementThreshold"], 100);

            Context ctx = ((AndroidControlWrapper)parent).Control.Context;
            _locMgr = ctx.GetSystemService(Context.LocationService) as LocationManager;

            Criteria locationCriteria = new Criteria();

            locationCriteria.Accuracy = Accuracy.Coarse;
            locationCriteria.PowerRequirement = Power.Medium;

            string locationProvider = _locMgr.GetBestProvider(locationCriteria, true);
            if (locationProvider != null)
            {
                if (_locMgr.IsProviderEnabled(locationProvider))
                {
                    logger.Info("Using best location provider: {0}", locationProvider);
                    _status = LocationStatus.Available;
                    _listener = new LocationListener(this);
                    _locMgr.RequestLocationUpdates(locationProvider, 2000, threshold, _listener);
                }
                else
                {
                    logger.Info("Best location provider: {0} - not enabled", locationProvider);
                    _status = LocationStatus.NotAvailable;
                }
            }
            else
            {
                // You will always get some kind of provider back if location services are
                // enabled, so this means that they are not enabled...
                //
                logger.Info("No location providers available");
                _status = LocationStatus.NotApproved;
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
                        { "latitude", new JValue(_location.Latitude) },
                        { "longitude", new JValue(_location.Longitude) }
                    });

                    if (_location.HasAccuracy)
                    {
                        obj.Add("accuracy", new JValue(_location.Accuracy));
                    }

                    /* Altitude, when provided, represents meters above the WGS 84 reference ellipsoid,
                     * which is of little or no utility to apps on our platform.  There also doesn't 
                     * appear to be any altittude accuracy on Android as there is on the othert platforms.
                     * 
                    if (_location.HasAltitude)
                    {
                        obj.Add("altitude", MaaasCore.JValue(_location.Altitude));
                    }
                     */

                    if (_location.HasBearing)
                    {
                        obj.Add("heading", new JValue(_location.Bearing));
                    }

                    if (_location.HasSpeed)
                    {
                        obj.Add("speed", new JValue(_location.Speed));
                    }

                    //_location.Time // UTC time, seconds since 1970
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

        public override void Unregister()
        {
            logger.Info("Location control unregistered, discontinuing location updates");
            if (_listener != null)
            {
                _locMgr.RemoveUpdates(_listener);
            }
            base.Unregister();
        }

        public void OnProviderEnabled(string provider)
        {
            logger.Info("Provider enabled: {0}", provider);
        }

        public void OnProviderDisabled(string provider)
        {
            logger.Info("Provider disabled: {0}", provider);
        }

        async public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            // !!! Are we going to get these for providers other than the one we're using?
            //
            // Availability.Available
            // Availability.OutOfService
            // Availability.TemporarilyUnavailable
            //
            logger.Info("Status change: {0}", status);
            if (status == Availability.Available)
            {
                if (_status != LocationStatus.Available)
                {
                    _status = LocationStatus.Available;
                }
            }
            else if ((status == Availability.OutOfService) || (status == Availability.TemporarilyUnavailable))
            {
                _status = LocationStatus.NotAvailable;
            }

            // Update the viewModel, and the server (if update on change specified)
            //
            updateValueBindingForAttribute("value");
            if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }

        async public void OnLocationChanged(Android.Locations.Location location)
        {
            logger.Info("Location change: {0}", location);
            _status = LocationStatus.Active;
            _location = location;

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
