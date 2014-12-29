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
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Graphics;
using Java.Lang;
using SynchroCore;

namespace SynchroClientAndroid.Controls
{
    public class MaaasRectDrawable : GradientDrawable
    {
        int _strokeWidth = 0;
        float _radius = 0;

        Color _fillColor;
        Color _strokeColor;

        public MaaasRectDrawable()
            : base()
        {
            this.SetShape(ShapeType.Rectangle);
            this.SetBounds(0, 0, 0, 0);
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
        static Logger logger = Logger.GetLogger("DrawableView");

        GradientDrawable _drawable;

        public DrawableView(Context context, GradientDrawable drawable)
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

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            logger.Debug("OnSizeChanged - w: {0} , h: {1}", w, h);
            base.OnSizeChanged(w, h, oldw, oldh);
            _drawable.SetBounds(0, 0, w, h);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            _drawable.Draw(canvas);
        }
    }

    class AndroidRectangleWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidRectangleWrapper");

        MaaasRectDrawable _rect = new MaaasRectDrawable();

        public AndroidRectangleWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating rectangle");

            DrawableView drawableView = new DrawableView(((AndroidControlWrapper)parent).Control.Context, _rect);
            this._control = drawableView;

            applyFrameworkElementDefaults(drawableView);

            processElementProperty(controlSpec["border"], value => _rect.SetStrokeColor(ToColor(value)));
            processElementProperty(controlSpec["borderThickness"], value => _rect.SetStrokeWidth((int)ToDeviceUnits(value)));
            processElementProperty(controlSpec["cornerRadius"], value => _rect.SetCornerRadius((float)ToDeviceUnits(value)));
            processElementProperty(controlSpec["fill"], value => _rect.SetFillColor(ToColor(value)));
        }
    }
}