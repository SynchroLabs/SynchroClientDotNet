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

        public ShapeView(Context context) : base(context)
        {
            int x = 0;
            int y = 0;
            int width = 160;
            int height = 160;

            _drawable = new ShapeDrawable(new RectShape());
            _drawable.SetBounds(x, y, x + width, y + height);
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
        }
    }
}