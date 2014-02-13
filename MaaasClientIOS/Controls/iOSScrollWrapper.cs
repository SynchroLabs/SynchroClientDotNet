using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace MaaasClientIOS.Controls
{
    class AutoSizingScrollView : UIScrollView
    {
        protected Orientation _orientation;

        public AutoSizingScrollView(Orientation orientation) : base()
        {
            _orientation = orientation;
        }

        public override void LayoutSubviews()
        {
            // this.Superview
            if (!Dragging && !Decelerating)
            {
                Util.debug("Laying out sub view");

                SizeF size = new SizeF(this.ContentSize);
                foreach (UIView view in this.Subviews)
                {
                    if (_orientation == Orientation.Vertical)
                    {
                        // Vertical scroll, size to content height
                        //
                        if ((view.Frame.Y + view.Frame.Height) > size.Height)
                        {
                            size.Height = view.Frame.Y + view.Frame.Height;
                        }
                    }
                    else
                    {
                        // Horizontal scroll, size to content width
                        //
                        if ((view.Frame.X + view.Frame.Width) > size.Width)
                        {
                            size.Width = view.Frame.X + view.Frame.Width;
                        }
                    }
                }
                this.ContentSize = size;
            }

            base.LayoutSubviews();
        }
    }

    class iOSScrollWrapper : iOSControlWrapper
    {
        public iOSScrollWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating scroll element");

            Orientation orientation = ToOrientation((string)controlSpec["orientation"], Orientation.Vertical);

            // https://developer.apple.com/library/ios/documentation/WindowsViews/Conceptual/UIScrollView_pg/Introduction/Introduction.html
            //
            UIScrollView scroller = new AutoSizingScrollView(orientation);
            this._control = scroller;

            processElementDimensions(controlSpec, 150, 50);
            applyFrameworkElementDefaults(scroller);

            if (controlSpec["contents"] != null)
            {
                createControls((JArray)controlSpec["contents"], (childControlSpec, childControlWrapper) =>
                {
                    scroller.AddSubview(childControlWrapper.Control);
                });
            }
        }
    }
}