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
    public class MaaasRectDrawable : GradientDrawable
    {
        int _width = 100;
        int _height = 100;
        int _strokeWidth = 0;
        float _radius = 0;

        Color _fillColor;
        Color _strokeColor;

        public MaaasRectDrawable()
            : base()
        {
            this.SetShape(ShapeType.Rectangle);
            this.SetBounds(0, 0, _width, _height);
        }

        public int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                this.SetBounds(0, 0, _width, _height);
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                this.SetBounds(0, 0, _width, _height);
            }
        }

        public void SetFillColor(Color color)
        {
            _fillColor = color;
            this.SetColor(_fillColor);
        }

        public void SetStrokeWidth(int width)
        {
            _strokeWidth = width;
        }

        public override void SetCornerRadius(float radius)
        {
            _radius = radius;
            base.SetCornerRadius(_radius);
        }

        public void SetStrokeColor(Color color)
        {
            _strokeColor = color;
        }

        public override void Draw(Canvas canvas)
        {
            // Since the stroke width and color can be set independantly, we update the stroke here before drawing...
            //
            this.SetStroke(_strokeWidth, _strokeColor);
            base.Draw(canvas);
        }
    }

    public class DrawableView : View
    {
        Drawable _drawable;

        public DrawableView(Context context, Drawable drawable)
            : base(context)
        {
            _drawable = drawable;
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

            MaaasRectDrawable rect = new MaaasRectDrawable();
            DrawableView drawableView = new DrawableView(((AndroidControlWrapper)parent).Control.Context, rect);
            this._control = drawableView;

            applyFrameworkElementDefaults(drawableView);
            processElementProperty((string)controlSpec["border"], value => rect.SetStrokeColor(ToColor(value)));
            processElementProperty((string)controlSpec["borderthickness"], value => rect.SetStrokeWidth((int)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["cornerradius"], value => rect.SetCornerRadius((float)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["fill"], value => rect.SetFillColor(ToColor(value)));

            // !!! The View needs to report its height/width for layout purposes (which values need to be updated via the setters below)
            //
            processElementProperty((string)controlSpec["width"], value => rect.Width = (int)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["height"], value => rect.Height = (int)ToDeviceUnits(value));
        }
    }
}