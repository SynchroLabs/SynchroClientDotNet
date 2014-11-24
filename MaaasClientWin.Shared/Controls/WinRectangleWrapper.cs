﻿using MaaasCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace MaaasClientWin.Controls
{
    class WinRectangleWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinRectangleWrapper");

        public WinRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating rectangle element");
            Rectangle rect = new Rectangle();
            this._control = rect;

            applyFrameworkElementDefaults(rect);
            processElementProperty(controlSpec["border"], value => rect.Stroke = ToBrush(value));
            processElementProperty(controlSpec["borderThickness"], value => rect.StrokeThickness = (float)ToDeviceUnits(value));
            processElementProperty(controlSpec["cornerRadius"], value => 
            {
                rect.RadiusX = (float)ToDeviceUnits(value);
                rect.RadiusY = (float)ToDeviceUnits(value);
            });
            processElementProperty(controlSpec["fill"], value => rect.Fill = ToBrush(value));
        }
    }
}
