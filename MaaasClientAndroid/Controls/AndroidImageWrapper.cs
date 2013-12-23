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
                image.LayoutParameters = new LinearLayout.LayoutParams(128, 128);
            }
            // image.SetScaleType(ImageView.ScaleType.FitXy);
            //image.LayoutParameters.Height = 64;

            // !!! image.SetMaxHeight(64); // Sizes will be overriden by the generic height/width property handlers, but
            // !!! image.SetMaxWidth(64);  // we have to set these here (as defaults) in case the sizes aren't specified. 

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