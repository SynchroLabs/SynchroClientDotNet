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
            processElementProperty((string)controlSpec["border"], value => _rect.SetStrokeColor(ToColor(value)));
            processElementProperty((string)controlSpec["borderThickness"], value =>
            {
                _thickness = (int)ToDeviceUnits(value);
                _rect.SetStrokeWidth(_thickness);
                this.updateLayoutPadding();
            });
            processElementProperty((string)controlSpec["cornerRadius"], value => _rect.SetCornerRadius((float)ToDeviceUnits(value)));
            processElementProperty((string)controlSpec["background"], value => _rect.SetFillColor(ToColor(value)));
            processElementProperty((string)controlSpec["padding"], value =>   // !!! Simple value only for now (would actually support complex values)
            {
                _padding = (int)ToDeviceUnits(value);
                this.updateLayoutPadding();
            });

            processElementProperty((string)controlSpec["alignContentH"], value => _layout.SetHorizontalGravity(ToHorizontalAlignment(value, GravityFlags.Center)), GravityFlags.Center);
            processElementProperty((string)controlSpec["alignContentV"], value => _layout.SetVerticalGravity(ToVerticalAlignment(value, GravityFlags.Center)), GravityFlags.Center);

            // In theory we're only jamming one child in here (so it doesn't really matter whether the linear layout is
            // horizontal or vertical.
            //
            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
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