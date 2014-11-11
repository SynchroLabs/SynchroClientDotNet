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
using MaaasCore;
using Newtonsoft.Json.Linq;
using Android.Locations;

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

        bool _updateOnChange = false;

        LocationManager _locMgr;
        LocationListener _listener;

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
                    _listener = new LocationListener(this);
                    _locMgr.RequestLocationUpdates(locationProvider, 2000, threshold, _listener);
                }
                else
                {
                    logger.Info("Location provider {0} not enabled", locationProvider);
                }
            }
            else
            {
                logger.Info("No location providers available");
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            processElementBoundValue("value", (string)bindingSpec["value"], () =>
            {
                JObject obj = new JObject(
                    new JProperty("latitude", _location.Latitude),
                    new JProperty("longitude", _location.Longitude)
                );

                if (_location.HasAccuracy)
                {
                    obj.Add(new JProperty("accuracy", _location.Accuracy));
                }

                /* Altitude, when provided, represents meters above the WGS 84 reference ellipsoid,
                 * which is of little or no utility to apps on our platform.
                 * 
                if (_location.HasAltitude)
                {
                    obj.Add(new JProperty("altitude", _location.Altitude));
                }
                 */

                if (_location.HasBearing)
                {
                    obj.Add(new JProperty("heading", _location.Bearing));
                }

                if (_location.HasSpeed)
                {
                    obj.Add(new JProperty("speed", _location.Speed));
                }

                //_location.Time // UTC time, seconds since 1970

                return obj;
            });

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }
        }

        public override void Unregister()
        {
            logger.Info("Location control unregistered, discontinuing location updates");
            _locMgr.RemoveUpdates(_listener);
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

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            logger.Info("Status change: {0}", status);
        }

        async public void OnLocationChanged(Android.Locations.Location location)
        {
            logger.Info("Location change: {0}", location);
            _location = location;

            updateValueBindingForAttribute("value");
            if (_updateOnChange)
            {
                await this.StateManager.sendUpdateRequestAsync();
            }
        }
    }
}
