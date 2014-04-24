using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    // Alignment, margins, and padding overview...
    //
    // http://msdn.microsoft.com/en-us/library/ms751709(v=vs.110).aspx
    //

    class WinStackPanelWrapper : WinControlWrapper
    {
        Border _border;
        Grid _grid;

        public WinStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
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

            Orientation orientation = ToOrientation(controlSpec["orientation"], Orientation.Vertical);

            // !!! For debug - show outline...
            //
            //_border.BorderBrush = ToBrush("White");
            //_border.BorderThickness = new Thickness(2);

            processThicknessProperty(controlSpec["padding"], () => _border.Padding, value => _border.Padding = (Thickness)value);

            if (controlSpec["contents"] != null)
            {
                int index = 0;
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    if (orientation == Orientation.Horizontal)
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
            }
        }
    }
}
