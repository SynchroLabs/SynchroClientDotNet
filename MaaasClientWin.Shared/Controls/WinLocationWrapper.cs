using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Geolocation;

namespace MaaasClientWin.Controls
{
    class WinLocationWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinLocationWrapper");

        Geolocator _geo;

        public WinLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            this._isVisualElement = false;

            _geo = new Geolocator();
            if (_geo != null)
            {
                logger.Info("Geolcator status on init: {0}", _geo.LocationStatus);
                //Geoposition pos = await _geo.GetGeopositionAsync();
                _geo.PositionChanged += geo_PositionChanged;
                _geo.StatusChanged += geo_StatusChanged;
            }
        }

        void geo_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            logger.Info("Geo status: {0}", args.Status);
            logger.Info("Geolcator status on status change: {0}", _geo.LocationStatus);
        }

        void geo_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            logger.Info("Latitude: {0}", args.Position.Coordinate.Point.Position.Latitude.ToString()); // -90 - 90
            logger.Info("Longitude: {0}", args.Position.Coordinate.Point.Position.Longitude.ToString()); // -180 - 180
            logger.Info("Accuracy: {0} meters", args.Position.Coordinate.Accuracy.ToString());
        }
    }
}
