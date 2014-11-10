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

        public LocationListener(AndroidLocationWrapper locationWrapper) : base()
        {
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

        public void OnLocationChanged(Android.Locations.Location location)
        {
            logger.Info("Location change: {0}", location);
        }
    }

    class AndroidLocationWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidLocationWrapper");

        LocationManager _locMgr;

        public AndroidLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            this._isVisualElement = false;

            Context ctx = ((AndroidControlWrapper)parent).Control.Context;
            _locMgr = ctx.GetSystemService(Context.LocationService) as LocationManager;

            Criteria locationCriteria = new Criteria();

            locationCriteria.Accuracy = Accuracy.Coarse;
            locationCriteria.PowerRequirement = Power.Medium;

            string locationProvider = _locMgr.GetBestProvider(locationCriteria, true);
            if (locationProvider != null)
            {
                _locMgr.RequestLocationUpdates(locationProvider, 2000, 1, new LocationListener(this)); // min time, min dist
                //_locMgr.RemoveUpdates(listener)
                //_locMgr.RequestSingleUpdate(locationCriteria, pendingIntent);
            }
            else
            {
                logger.Info("No location providers available");
            }
        }
    }
}
