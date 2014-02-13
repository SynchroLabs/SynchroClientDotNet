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
using Java.Lang;

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

    public class DrawableView : View, Drawable.ICallback
    {
        Drawable _drawable;

        public DrawableView(Context context, Drawable drawable)
            : base(context)
        {
            _drawable = drawable;
            _drawable.SetCallback(this);
        }

        public override void InvalidateDrawable(Drawable who) 
        {
            base.InvalidateDrawable(who);
            this.Invalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            _drawable.Draw(canvas);
        }
    }

    class AndroidRectangleWrapper : AndroidControlWrapper
    {
        MaaasRectDrawable _rect = new MaaasRectDrawable();

        public AndroidRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating rectangle");

            DrawableView drawableView = new DrawableView(((AndroidControlWrapper)parent).Control.Context, _rect);
            this._control = drawableView;

            drawableView.LayoutChange += drawableView_LayoutChange;

            applyFrameworkElementDefaults(drawableView);

            processElementProperty((string)controlSpec["border"], value => _rect.SetStrokeColor(ToColor(value)));
            processElementProperty((string)controlSpec["borderThickness"], value => _rect.SetStrokeWidth((int)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["cornerRadius"], value => _rect.SetCornerRadius((float)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["fill"], value => _rect.SetFillColor(ToColor(value)));
        }

        void drawableView_LayoutChange(object sender, View.LayoutChangeEventArgs e)
        {
            _rect.Width = this.Width;
            _rect.Height = this.Height;
        }
    }
}