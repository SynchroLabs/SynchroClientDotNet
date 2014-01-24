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

namespace MaaasClientAndroid.Controls
{
    public class BorderPaddingThicknessSetter : ThicknessSetter
    {
        protected View _control;
        protected int _paddingLeft = 0;
        protected int _paddingTop = 0;
        protected int _paddingRight = 0;
        protected int _paddingBottom = 0;
        protected int _inset = 0;

        public BorderPaddingThicknessSetter(View control)
        {
            _control = control;
            _paddingLeft = _control.PaddingLeft;
            _paddingTop = _control.PaddingTop;
            _paddingRight = _control.PaddingRight;
            _paddingBottom = _control.PaddingBottom;
        }

        public int Inset 
        {
            get { return _inset; }
            set
            {
                _inset = value;
                updatePadding();
            }
        }

        protected void updatePadding()
        {
            _control.SetPadding(_paddingLeft + _inset, _paddingTop + _inset, _paddingRight + _inset, _paddingBottom + _inset);
        }

        public override void SetThickness(int thickness)
        {
            _paddingLeft = thickness;
            _paddingTop = thickness;
            _paddingRight = thickness;
            _paddingBottom = thickness;
            updatePadding();
        }

        public override void SetThicknessLeft(int thickness)
        {
            _paddingLeft = thickness;
            updatePadding();
        }

        public override void SetThicknessTop(int thickness)
        {
            _paddingTop = thickness;
            updatePadding();
        }

        public override void SetThicknessRight(int thickness)
        {
            _paddingRight = thickness;
            updatePadding();
        }

        public override void SetThicknessBottom(int thickness)
        {
            _paddingBottom = thickness;
            updatePadding();
        }
    }

    class AndroidBorderWrapper : AndroidControlWrapper
    {
        LinearLayout _layout;
        int _padding = 0;
        int _thickness = 0;

        MaaasRectDrawable _rect = new MaaasRectDrawable();

        protected void updateLayoutPadding()
        {
            _layout.SetPadding(
                _padding + _thickness,
                _padding + _thickness,
                _padding + _thickness,
                _padding + _thickness
                );
        }

        public AndroidBorderWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating border element");

            _layout = new LinearLayout(((AndroidControlWrapper)parent).Control.Context);
            this._control = _layout;

            _layout.SetBackgroundDrawable(_rect);
            _layout.LayoutChange += _layout_LayoutChange;

            applyFrameworkElementDefaults(_layout);

            // If border thickness or padding change, need to record value and update layout padding...
            //
            BorderPaddingThicknessSetter borderThicknessSetter = new BorderPaddingThicknessSetter(this.Control);
            processElementProperty((string)controlSpec["border"], value => _rect.SetStrokeColor(ToColor(value)));
            processElementProperty((string)controlSpec["borderThickness"], value =>
            {
                _thickness = (int)ToDeviceUnits(value);
                _rect.SetStrokeWidth(_thickness);
                borderThicknessSetter.Inset = _thickness;
            });
            processElementProperty((string)controlSpec["cornerRadius"], value => _rect.SetCornerRadius((float)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["background"], value => _rect.SetFillColor(ToColor(value)));
            processThicknessProperty(controlSpec["padding"], borderThicknessSetter);

            processElementProperty((string)controlSpec["alignContentH"], value => _layout.SetHorizontalGravity(ToHorizontalAlignment(value, GravityFlags.Center)), GravityFlags.Center);
            processElementProperty((string)controlSpec["alignContentV"], value => _layout.SetVerticalGravity(ToVerticalAlignment(value, GravityFlags.Center)), GravityFlags.Center);

            // In theory we're only jamming one child in here (so it doesn't really matter whether the linear layout is
            // horizontal or vertical.
            //
            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    LinearLayout.LayoutParams layoutParams = null;

                    if (childControlWrapper.Control.LayoutParameters != null)
                    {
                        if (childControlWrapper.Control.LayoutParameters is ViewGroup.MarginLayoutParams)
                        {
                            layoutParams = new LinearLayout.LayoutParams((ViewGroup.MarginLayoutParams)childControlWrapper.Control.LayoutParameters);
                        }
                        else
                        {
                            layoutParams = new LinearLayout.LayoutParams(childControlWrapper.Control.LayoutParameters);
                        }
                    }
                    else
                    {
                        layoutParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                    }

                    childControlWrapper.Control.LayoutParameters = layoutParams;

                    _layout.AddView(childControlWrapper.Control);
                });
            }
        }

        void _layout_LayoutChange(object sender, View.LayoutChangeEventArgs e)
        {
            _rect.Height = _layout.Height;
            _rect.Width = _layout.Width;
        }
    }
}