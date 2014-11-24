using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    // Alignment, margins, and padding overview...
    //
    // http://msdn.microsoft.com/en-us/library/ms751709(v=vs.110).aspx
    //

    class GridLengths
    {
        public GridLengths() {}
        public GridLength Width { get; set; }
        public GridLength Height { get; set; }
    }

    class SynchroGrid : Grid
    {
        static Logger logger = Logger.GetLogger("SynchroGrid");

        // http://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.frameworkelement.arrangeoverride
        //
        protected override Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            // logger.Log(LogLevel.Info, "ArrangeOverride on grid with {0} children", this.Children.Count);
            //
            foreach (FrameworkElement element in this.Children)
            {
                // When we have a child that is invisible, we have to force the Grid view not to reserve space for it (by hammering over the 
                // appropriate GridLength values with zero).  Otherwise, at least in the case of star spacing, it will still allocate
                // a proportion of the overall space for the child, even though it is invisible.  Admittedly, the "invisible star child" is a
                // special case, but an important one for layout purposes.
                //

                // logger.Log(LogLevel.Info, "Arrange - Child in row {0}, col {1} with desired size: {2}", Grid.GetRow(element), Grid.GetColumn(element), element.DesiredSize);
                //
                if ((element.Visibility == Visibility.Collapsed) && (element.Tag == null))
                {
                    // logger.Log(LogLevel.Info, "Setting height to 0 pixels");
                    //
                    GridLengths gl = new GridLengths();
                    RowDefinition rowDef = this.RowDefinitions[Grid.GetRow(element)];
                    if (rowDef != null)
                    {
                        gl.Height = rowDef.Height;
                        rowDef.Height = new GridLength(0, GridUnitType.Pixel);
                    }
                    ColumnDefinition colDef = this.ColumnDefinitions[Grid.GetColumn(element)];
                    if (colDef != null)
                    {
                        gl.Width = colDef.Width;
                        colDef.Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    element.Tag = gl;
                }
                else if ((element.Visibility != Visibility.Collapsed) && (element.Tag != null))
                {
                    // logger.Log(LogLevel.Info, "Setting height back");
                    //
                    GridLengths gl = (GridLengths)element.Tag;
                    RowDefinition rowDef = this.RowDefinitions[Grid.GetRow(element)];
                    if (rowDef != null)
                    {
                        rowDef.Height = gl.Height;
                    }
                    ColumnDefinition colDef = this.ColumnDefinitions[Grid.GetColumn(element)];
                    if (colDef != null)
                    {
                        colDef.Width = gl.Width;
                    }
                    element.Tag = null;
                }
            }

            return base.ArrangeOverride(finalSize);
        }
    }

    class WinStackPanelWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinStackPanelWrapper");

        Border _border;
        Grid _grid;
        Orientation _orientation;

        public WinStackPanelWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating stackpanel element");

            // In order to get padding support, we put a Border around the StackPanel...
            //
            _border = new Border();

            // In order to be able to distribute "extra" space between controls (required for flexible layout), we had
            // to move away from the Windows StackPanel (which can't do that) and move to the Grid (which can).  So now
            // every stack panel is actually a Grid (with a single row or column, depending on orientation).
            //
            _grid = new SynchroGrid();
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

                        Grid.SetRow(childControlWrapper.Control, 0);
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
                        Grid.SetColumn(childControlWrapper.Control, 0);
                    }

                    _grid.Children.Add(childControlWrapper.Control);

                    index++;
                });

                processElementProperty(controlSpec["orientation"], value => UpdateOrientation(ToOrientation(value, _orientation)));
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
