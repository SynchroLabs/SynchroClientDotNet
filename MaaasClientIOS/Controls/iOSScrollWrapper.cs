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
        public AutoSizingScrollView() : base()
        {
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
                    if ((view.Frame.X + view.Frame.Width) > size.Width)
                    {
                        size.Width = view.Frame.X + view.Frame.Width;
                    }

                    if ((view.Frame.Y + view.Frame.Height) > size.Height)
                    {
                        size.Height = view.Frame.Y + view.Frame.Height;
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

            // https://developer.apple.com/library/ios/documentation/WindowsViews/Conceptual/UIScrollView_pg/Introduction/Introduction.html
            //
            UIScrollView scroller = new AutoSizingScrollView(); // UIScrollView();
            this._control = scroller;

            Orientation orientation = ToOrientation((string)controlSpec["orientation"], Orientation.Vertical);
            if (orientation == Orientation.Vertical)
            {
                // !!! Vertical (default)
            }
            else
            {
                // !!! Horizontal
            }

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