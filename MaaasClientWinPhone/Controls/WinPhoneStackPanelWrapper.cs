using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneStackPanelWrapper : WinPhoneControlWrapper
    {
        Border _border;
        Grid _grid;

        Orientation _orientation;

        public WinPhoneStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating stackpanel element");

            // In order to get padding support, we put a Border around the StackPanel...
            //
            _border = new Border();

            // In order to be able to distribute "extra" space between controls (required for flexible layout), we had
            // to move away from the Windows StackPanel (which can't do that) and move to the Grid (which can).  So now
            // every stack panel is actually a Grid (with a single row or column, depending on orientation).
            //
            _grid = new Grid();
            _border.Child = _grid;
            this._control = _border;

            applyFrameworkElementDefaults(_border, false);

            processThicknessProperty(controlSpec["padding"], () => _border.Padding, value => _border.Padding = (Thickness)value);

            // This is a little weird.  We're going to attempt to resolve the orientation value now, because we must have a default orientation to use
            // in the initial layout code below (as we add child controls).  Below all of this we will *also* bind to the orientation so that it can be
            // updated dynamically.
            //
            _orientation = ToOrientation(controlSpec["orientation"], Orientation.Vertical);

            if (controlSpec["contents"] != null)
            {
                int index = 0;
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        ColumnDefinition colDef = new ColumnDefinition();

                        int starCount = GetStarCount((string)childControlSpec["width"]);
                        if (starCount > 0)
                        {
                            colDef.Width = new GridLength(starCount, GridUnitType.Star);
                        }
                        else
                        {
                            colDef.Width = new GridLength(1, GridUnitType.Auto);
                        }

                        _grid.ColumnDefinitions.Add(colDef);

                        Grid.SetColumn(childControlWrapper.Control, index);
                    }
                    else
                    {
                        RowDefinition rowDef = new RowDefinition();

                        int starCount = GetStarCount((string)childControlSpec["height"]);
                        if (starCount > 0)
                        {
                            rowDef.Height = new GridLength(starCount, GridUnitType.Star);
                        }
                        else
                        {
                            rowDef.Height = new GridLength(1, GridUnitType.Auto);
                        }

                        _grid.RowDefinitions.Add(rowDef);

                        Grid.SetRow(childControlWrapper.Control, index);
                    }

                    _grid.Children.Add(childControlWrapper.Control);

                    index++;
                });

                processElementProperty((string)controlSpec["orientation"], value => UpdateOrientation(ToOrientation(value, _orientation)));
            }
        }

        public void UpdateOrientation(Orientation orientation)
        {
            if (orientation != _orientation)
            {
                if (orientation == Orientation.Horizontal)
                {
                    _grid.ColumnDefinitions.Clear();
                    foreach (var rowDef in _grid.RowDefinitions)
                    {
                        ColumnDefinition colDef = new ColumnDefinition();
                        colDef.Width = rowDef.Height;
                        _grid.ColumnDefinitions.Add(colDef);
                    }

                    foreach (FrameworkElement child in _grid.Children)
                    {
                        Grid.SetColumn(child, Grid.GetRow(child));
                        Grid.SetRow(child, 0);
                    }

                    _grid.RowDefinitions.Clear();
                }
                else
                {
                    _grid.RowDefinitions.Clear();
                    foreach (var colDef in _grid.ColumnDefinitions)
                    {
                        RowDefinition rowDef = new RowDefinition();
                        rowDef.Height = colDef.Width;
                        _grid.RowDefinitions.Add(rowDef);
                    }

                    foreach (FrameworkElement child in _grid.Children)
                    {
                        Grid.SetRow(child, Grid.GetColumn(child));
                        Grid.SetColumn(child, 0);
                    }

                    _grid.ColumnDefinitions.Clear();
                }

                _orientation = orientation;
            }
        }
    }
}