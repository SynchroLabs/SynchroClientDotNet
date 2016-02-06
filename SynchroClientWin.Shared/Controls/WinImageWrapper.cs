using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MaaasClientWin.Controls
{
    class WinImageWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinImageWrapper");

        static string[] Commands = new string[] { CommandName.OnTap.Attribute };

        public Stretch ToImageScaleMode(JToken value, Stretch defaultMode = Stretch.Uniform)
        {
            Stretch scaleMode = defaultMode;
            string scaleModeValue = ToString(value);
            if (scaleModeValue == "Stretch")
            {
                scaleMode = Stretch.Fill;
            }
            else if (scaleModeValue == "Fit")
            {
                scaleMode = Stretch.Uniform;
            }
            else if (scaleModeValue == "Fill")
            {
                scaleMode = Stretch.UniformToFill;
            }
            return scaleMode;
        }

        public WinImageWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating image element");
            Image image = new Image();
            this._control = image;

            // Image scaling via image.stretch
            //
            //     Stretch.Fill;         -  "Stretch" to fill 
            //     Stretch.Uniform;      -  "Fit" preserving aspect
            //     Stretch.UniformToFill -  "Fill" preserving aspect
            //
            // Note: When using Fit, the horizontal and vertical alignment on the control will determine how the
            //       images is positioned withing the control space.  When using Fill, it *should* work the same way,
            //       but does not (the docs and past examples indicate that setting h/v align to "center" would center
            //       the image and clip the edges on the sides that overflow, but in practice the image is always
            //       anchored at the top/left).  We could probably work around this with another viewport control containing
            //       the image, but that's more complexity than it's worth.
            //
            processElementProperty(controlSpec, "scale", value => image.Stretch = ToImageScaleMode(value));

            applyFrameworkElementDefaults(image);
            image.Height = 128; // Sizes will be overriden by the generic height/width property handlers, but
            image.Width = 128;  // we have to set these here (as defaults) in case the sizes aren't specified.
 
            processElementProperty(controlSpec, "resource", value => 
            {
                String img = ToString(value);
                if (String.IsNullOrEmpty(img))
                {
                    image.Source = null;
                }
                else
                {
                    image.Source = new BitmapImage(new Uri(img));
                }
            });

            image.ImageOpened += (sender, e) =>
            {
                BitmapImage bitmap = (image.Source as BitmapImage);
                logger.Debug("Image Loaded - h: {0}, w: {1}", bitmap.PixelHeight, bitmap.PixelWidth);

                // The idea is that if the size of the control was only specified in one dimension, then we will use
                // the aspect ratio of the loaded image to determine and set the size in the other dimension appropriately.
                // In this case, it doesn't really matter what the scale is set to, since the image will fit exactly.
                //
                if (_heightSpecified && !_widthSpecified)
                {
                    // Only height specified, set width based on image aspect
                    //
                    image.Width = bitmap.PixelWidth / (double)bitmap.PixelHeight * image.Height;
                }
                else if (_widthSpecified && !_heightSpecified)
                {
                    // Only width specified, set height based on image aspect
                    //
                    image.Height = bitmap.PixelHeight / (double)bitmap.PixelWidth * image.Width;
                }
            };

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, CommandName.OnTap.Attribute, Commands);
            ProcessCommands(bindingSpec, Commands);

            if (GetCommand(CommandName.OnTap) != null)
            {
                image.Tapped += image_Tapped;
            }

        }

        async void image_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CommandInstance command = GetCommand(CommandName.OnTap);
            if (command != null)
            {
                logger.Debug("Image tapped with command: {0}", command);
                await this.StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(BindingContext));
            }
        }
    }
}
