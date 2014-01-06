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
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Graphics;

namespace MaaasClientAndroid.Controls
{

    public class ShapeView : View 
    {
        private ShapeDrawable _drawable;

        int _width = 0;
        int _height = 0;

        public ShapeView(Context context) : base(context)
        {
            _width = 100;
            _height = 100;

            _drawable = new ShapeDrawable(new RectShape());
            _drawable.SetBounds(0, 0, _width, _height);
        }

        public int ShapeWidth 
        { 
            get { return _width; }
            set 
            {
                _width = value;
                _drawable.SetBounds(0, 0, _width, _height);
            }
        }

        public int ShapeHeight
        {
            get { return _height; }
            set
            {
                _height = value;
                _drawable.SetBounds(0, 0, _width, _height);
            }
        }

        public void SetFillColor(Color color)
        {
            _drawable.Paint.Color = color;
        }

        protected override void OnDraw(Canvas canvas) 
        {
            _drawable.Draw(canvas);
        }
    }

    class AndroidRectangleWrapper : AndroidControlWrapper
    {
        public AndroidRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating rectangle");

            ShapeView shapeView = new ShapeView(((AndroidControlWrapper)parent).Control.Context);
            this._control = shapeView;

            applyFrameworkElementDefaults(shapeView);
            processElementProperty((string)controlSpec["fill"], value => shapeView.SetFillColor(ToColor(value)));
            processElementProperty((string)controlSpec["width"], value => shapeView.ShapeWidth = (int)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["height"], value => shapeView.ShapeHeight = (int)ToDeviceUnits(value));
        }
    }
}