using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Geolocation;
using Windows.UI.Core;

namespace MaaasClientWin.Controls
{
    class WinLocationWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinLocationWrapper");

        bool _updateOnChange = false;

        Geolocator _geo;
        Geoposition _position;

        CoreDispatcher _dispatcher;

        public WinLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            this._isVisualElement = false;

            _dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            int threshold = (int)ToDouble(controlSpec["movementThreshold"], 100);

            _geo = new Geolocator();
            if (_geo != null)
            {
                logger.Info("Geolcator status on init: {0}", _geo.LocationStatus);
                _geo.DesiredAccuracyInMeters = 100;
                _geo.ReportInterval = 2000;
                _geo.MovementThreshold = threshold;
                _geo.PositionChanged += geo_PositionChanged;
                _geo.StatusChanged += geo_StatusChanged;
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
            processElementBoundValue("value", (string)bindingSpec["value"], () => 
            {
                JObject obj = new JObject(
                    new JProperty("latitude", _position.Coordinate.Point.Position.Latitude),
                    new JProperty("longitude", _position.Coordinate.Point.Position.Longitude),
                    new JProperty("accuracy", _position.Coordinate.Accuracy)
                );

                /*
                 * Altitude is too ambiguous to be of any real use in our apps, due to the variety
                 * of different types of altitude representations provided.  For more information, 
                 * see: Geoposition.Coordinate.Point.AltitudeReferenceSystem 
                 *
                if (_position.Coordinate.AltitudeAccuracy != null)
                {
                    obj.Add(new JProperty("altitude", _position.Coordinate.Point.Position.Altitude));
                    obj.Add(new JProperty("altitudeAccuracy", _position.Coordinate.AltitudeAccuracy));
                }
                 */

                if (_position.Coordinate.Heading != null)
                {
                    obj.Add(new JProperty("heading", _position.Coordinate.Heading));
                }

                if (_position.Coordinate.Speed != null)
                {
                    obj.Add(new JProperty("speed", _position.Coordinate.Speed));
                }

                // _position.Coordinate.Timestamp // DateTimeOffset

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
            _geo.PositionChanged -= geo_PositionChanged;
            _geo.StatusChanged -= geo_StatusChanged;
            base.Unregister();
        }
        
        void geo_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            // "Initializing"
            // "Ready"
            logger.Info("Geo status: {0}", args.Status);
            logger.Info("Geolcator status on status change: {0}", _geo.LocationStatus);
        }

        async void geo_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            logger.Info(
                "Location lat: {0}, long: {1}, accuracy: {2}", 
                args.Position.Coordinate.Point.Position.Latitude, 
                args.Position.Coordinate.Point.Position.Longitude,
                args.Position.Coordinate.Accuracy
                );

            _position = args.Position;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                updateValueBindingForAttribute("value");
                if (_updateOnChange)
                {
                    await this.StateManager.sendUpdateRequestAsync();
                }
            });
        }
    }
}
