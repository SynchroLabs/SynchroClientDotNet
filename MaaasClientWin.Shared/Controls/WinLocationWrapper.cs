using SynchroCore;
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

        static string[] Commands = new string[] { CommandName.OnUpdate.Attribute };

        bool _updateOnChange = false;

        Geolocator _geo;

        LocationStatus _status = LocationStatus.Unknown;
        Geoposition _location;

        CoreDispatcher _dispatcher;

        public WinLocationWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating location element");
            this._isVisualElement = false;

            _dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            int threshold = (int)ToDouble(controlSpec["movementThreshold"], 100);

            _geo = new Geolocator();
            if (_geo.LocationStatus == PositionStatus.Disabled)
            {
                _status = LocationStatus.NotApproved;
            }
            else if (_geo.LocationStatus == PositionStatus.NotAvailable)
            {
                _status = LocationStatus.NotAvailable;
                _geo = null;
            }
            else
            {
                _status = LocationStatus.DeterminingAvailabily;
                _geo.DesiredAccuracyInMeters = 100;
                _geo.ReportInterval = 2000;
                _geo.MovementThreshold = threshold;
                _geo.PositionChanged += geo_PositionChanged;
                _geo.StatusChanged += geo_StatusChanged;
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value", Commands);
            ProcessCommands(bindingSpec, Commands);

            processElementBoundValue("value", (string)bindingSpec["value"], () => 
            {
                JObject obj = new JObject()
                {
                    { "available", new JValue(((_status == LocationStatus.Available) || (_status == LocationStatus.Active))) },
                    { "status", new JValue(_status.ToString()) }
                };

                if (_location != null)
                {
                    obj.Add("coordinate", new JObject()
                    {
                        { "latitude", new JValue(_location.Coordinate.Point.Position.Latitude) },
                        { "longitude", new JValue(_location.Coordinate.Point.Position.Longitude) }
                    });

                    obj.Add("accuracy", new JValue(_location.Coordinate.Accuracy));

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

                    if (!double.IsNaN(_location.Coordinate.Heading.GetValueOrDefault(double.NaN)))
                    {
                        obj.Add("heading", new JValue(_location.Coordinate.Heading));
                    }

                    if (!double.IsNaN(_location.Coordinate.Speed.GetValueOrDefault(double.NaN)))
                    {
                        obj.Add("speed", new JValue(_location.Coordinate.Speed));
                    }

                    // _position.Coordinate.Timestamp // DateTimeOffset
                }

                return obj;
            });

            if ((string)bindingSpec["sync"] == "change")
            {
                _updateOnChange = true;
            }

            // This triggers the viewModel update
            //
            updateValueBindingForAttribute("value");
        }

        protected void stopLocationServices()
        {
            if (_geo != null)
            {
                _geo.PositionChanged -= geo_PositionChanged;
                _geo.StatusChanged -= geo_StatusChanged;
            }
            _geo = null;
        }

        public override void Unregister()
        {
            logger.Info("Location control unregistered, discontinuing location updates");
            this.stopLocationServices();
            base.Unregister();
        }
        
        async void geo_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            // When it's going to work we see:
            //
            //     NoData (sometimes)
            //     Initializing
            //     Ready (immediately before PositionChanged notification)
            //
            logger.Info("Geolcator status on status change: {0}", _geo.LocationStatus);

            if ((_geo.LocationStatus == PositionStatus.Disabled) || (_geo.LocationStatus == PositionStatus.NotAvailable))
            {
                _status = LocationStatus.NotAvailable;
                this.stopLocationServices();

                // Update the viewModel, and the server (if update on change specified)
                //
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

        async void geo_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            logger.Info(
                "Location lat: {0}, long: {1}, accuracy: {2}", 
                args.Position.Coordinate.Point.Position.Latitude, 
                args.Position.Coordinate.Point.Position.Longitude,
                args.Position.Coordinate.Accuracy
                );

            _status = LocationStatus.Active;
            _location = args.Position;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
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
            });
        }
    }
}
