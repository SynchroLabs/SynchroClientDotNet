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
using Android.Graphics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;
using ModernHttpClient;

namespace MaaasClientAndroid.Controls
{
    class AndroidImageWrapper : AndroidControlWrapper
    {
        public AndroidImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating image element");
            ImageView image = new ImageView(((AndroidControlWrapper)parent).Control.Context);
            this._control = image;

            applyFrameworkElementDefaults(image);

            if (image.LayoutParameters == null)
            {
                image.LayoutParameters = new ViewGroup.LayoutParams(0, 0);
            }

            processElementProperty((string)controlSpec["height"], value => image.LayoutParameters.Height = (int)ToDeviceUnits(value));
            processElementProperty((string)controlSpec["width"], value => image.LayoutParameters.Width = (int)ToDeviceUnits(value));

            // !!! Image scale type might be interesting later...
            //
            // image.SetScaleType(ImageView.ScaleType.FitXy);

            var ctx = SynchronizationContext.Current;
            processElementProperty((string)controlSpec["resource"], async value =>
            {
                Uri uri = this.StateManager.buildUri(ToString(value));
                await this.loadImage(ctx, uri);
            });
        }

        private async Task loadImage(SynchronizationContext ctx, Uri uri)
        {
            ImageView image = (ImageView)this._control;

            Util.debug("Getting read the try to load image from: " + uri);

            /*
            byte[] bytes = await Util.GetResponseBytes(uri);
            if (bytes != null)
            {
                ﻿var bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                ctx.Post(_ => { image.SetImageBitmap(bitmap); }, null);
            }
             */

            using (var client = new HttpClient(new OkHttpNetworkHandler()))
            {
                try
                {
                    Util.debug("Getting read the try to load image from: " + uri);

                    var msg = await client.GetAsync(uri);
                    if (msg.IsSuccessStatusCode)
                    {
                        using (var stream = await msg.Content.ReadAsStreamAsync())
                        {
                            ﻿var bitmap = await BitmapFactory.DecodeStreamAsync(stream);
                            image.SetImageBitmap(bitmap);
                        }
                    }
                }
                catch (Exception e)
                {
                    Util.debug("WebExceptioon caught, details: " +  e.Message);
                }
            }
        }
    }
}