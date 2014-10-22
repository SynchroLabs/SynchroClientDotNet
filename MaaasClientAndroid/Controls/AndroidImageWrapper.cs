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
using Java.Net;
using Java.IO;

namespace SynchroClientAndroid.Controls
{
    class AndroidImageWrapper : AndroidControlWrapper
    {
        static Logger logger = Logger.GetLogger("AndroidImageWrapper");

        public AndroidImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            logger.Debug("Creating image element");
            ImageView image = new ImageView(((AndroidControlWrapper)parent).Control.Context);
            this._control = image;

            applyFrameworkElementDefaults(image);

            // !!! Image scaling
            //
            // image.SetScaleType(ImageView.ScaleType.FitXy);        // Stretch to fill 
            // image.SetScaleType(ImageView.ScaleType.CenterCrop);   // Fill preserving aspect
            // image.SetScaleType(ImageView.ScaleType.CenterInside); // Fit preserving aspect

            var ctx = SynchronizationContext.Current;
            processElementProperty((string)controlSpec["resource"], value =>
            {
                if (value == null)
                {
                    image.SetImageDrawable(null);
                }
                else
                {
                    Uri uri = new Uri(ToString(value));
                    ThreadPool.QueueUserWorkItem(o => this.loadImage(ctx, uri));
                }
            });
        }

        // This is some test code (trying unsuccessfully to diagnose the sporadic java.net.UnknownHostException that comes
        // from HttpClient.getAsync (when trying to call GetAllByName() internally).  So far no information.  It fails a lot.
        //
        /*
        private void hostLookup(string host)
        {
            try
            {
                Java.Net.InetAddress address = Java.Net.Inet4Address.GetByName(host);
                logger.Debug("Inet4Address.GetByName success for: {0}", host);
            }
            catch (Exception e)
            {
                logger.Debug("Inet4Address.GetByName failed for: {0}", host);
            }

            try
            {
                Java.Net.InetAddress[] addresses = Java.Net.InetAddress.GetAllByName(host);
                logger.Debug("InetAddress.GetAllByName success for: {0}", host);
            }
            catch (Exception e)
            {
                logger.Debug("InetAddress.GetAllByName failed for: {0}", host);
            }
        }
         */

        // Note: This used to return a Task and be called using the standard async/await method.  The problem with
        //       this approach is that the called task is still on the main thread until the first "await".  Since
        //       network access is not allowed from the main thread, that means calls to blocking network code, such
        //       as doing a DNS lookup, would fail if attempted before the first async operation.  For this reason, we
        //       now execute this as a ThreadPool work item (so the entire method runs on a background thread).
        //
        private async void loadImage(SynchronizationContext ctx, Uri uri)
        {
            ImageView image = (ImageView)this._control;

            //hostLookup("maaas.blob.core.windows.net");

            // http://fbcdn-profile-a.akamaihd.net/hprofile-ak-ash3/c23.23.285.285/s160x160/943786_10201215910308278_1343091684_n.jpg
            // hostLookup("fbcdn-profile-a.akamaihd.net");

            logger.Debug("Getting read the try to load image from: {0}", uri);

            /*
	        try 
            {
		        URL url = new URL(uri.ToString());
		        HttpURLConnection connection = (HttpURLConnection)url.OpenConnection();
		        connection.DoInput = true;
		        connection.Connect();
                Bitmap bitmap = BitmapFactory.DecodeStream(connection.InputStream);
                ctx.Post(_ => { image.SetImageBitmap(bitmap); }, null);
            }
            catch (IOException e) 
            {
		        e.PrintStackTrace();
	        }
            */

            using (var client = new HttpClient(new OkHttpNetworkHandler()))
            {
                try
                {
                    logger.Debug("Getting read the try to load image from: {0}", uri);

                    var msg = await client.GetAsync(uri);
                    if (msg.IsSuccessStatusCode)
                    {
                        using (var stream = await msg.Content.ReadAsStreamAsync())
                        {
                            ﻿var bitmap = await BitmapFactory.DecodeStreamAsync(stream);
                             ctx.Post(_ => { image.SetImageBitmap(bitmap); }, null);
                             //image.SetImageBitmap(bitmap);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Warn("WebExceptioon caught, details: {0}", e.Message);
                }
            }
        }
    }
}