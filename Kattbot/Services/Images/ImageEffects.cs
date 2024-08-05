using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Path = System.IO.Path;

namespace Kattbot.Services.Images;

public static class ImageEffects
{
    public static Image ScaleImage(Image image, double scaleFactor)
    {
        var newWidth = (int)(image.Width * scaleFactor);
        var newHeight = (int)(image.Height * scaleFactor);

        image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

        return image;
    }

    public static Image DeepFryImage(Image image)
    {
        image.Mutate(
            i =>
            {
                i.Contrast(5f);
                i.Brightness(1.5f);
                i.GaussianSharpen(5f);
                i.Saturate(5f);
            });

        return image;
    }

    public static Image OilPaintImage(Image image)
    {
        const int paintLevel = 25;

        image.Mutate(i => { i.OilPaint(paintLevel, paintLevel); });

        return image;
    }

    /// <summary>
    ///     Twirls an image.
    ///     Algorithm source: jhlabs.com.
    /// </summary>
    /// <param name="src">Source image.</param>
    /// <param name="angleDeg">Angle in degrees.</param>
    /// <returns>Twirled image.</returns>
    public static Image TwirlImage(Image<Rgba32> src, float angleDeg = 180)
    {
        var dest = new Image<Rgba32>(src.Width, src.Height);

        int centerX = src.Width / 2;
        int centerY = src.Height / 2;
        int radius = Math.Min(centerX, centerY);
        int radius2 = radius * radius;
        var angleRad = (float)((angleDeg * Math.PI) / 180);

        for (var x = 0; x < src.Width; x++)
        {
            for (var y = 0; y < src.Height; y++)
            {
                (int X, int Y) trans = TransformFn(x, y);
                dest[x, y] = src[trans.X, trans.Y];
            }
        }

        return dest;

        (int X, int Y) TransformFn(int x, int y)
        {
            int newX = x;
            int newY = x;

            float dx = x - centerX;
            float dy = y - centerY;
            float distance = (dx * dx) + (dy * dy);

            if (!(distance <= radius2))
                return (X: newX, Y: newY);

            distance = (float)Math.Sqrt(distance);
            float a = (float)Math.Atan2(dy, dx) + ((angleRad * (radius - distance)) / radius);

            newX = (int)Math.Floor(centerX + (distance * (float)Math.Cos(a)));
            newY = (int)Math.Floor(centerY + (distance * (float)Math.Sin(a)));

            return (X: newX, Y: newY);
        }
    }

    public static Image<TPixel> CropToCircle<TPixel>(Image<TPixel> image)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        var ellipsePath = new EllipsePolygon(image.Width / 2.0f, image.Height / 2.0f, image.Width, image.Height);

        Image<TPixel> imageAsPngWithTransparency;

        if (image.Metadata.DecodedImageFormat is not PngFormat ||
            image.Metadata.GetPngMetadata().ColorType is not PngColorType.RgbWithAlpha)
        {
            using var stream = new MemoryStream();

            image.SaveAsPngAsync(stream, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });

            stream.Position = 0;

            imageAsPngWithTransparency = Image.Load<TPixel>(stream);
        }
        else
        {
            imageAsPngWithTransparency = image;
        }

        Image<TPixel> cloned = imageAsPngWithTransparency.Clone(
            i =>
            {
                var opts = new DrawingOptions
                {
                    GraphicsOptions = new GraphicsOptions
                    {
                        Antialias = true,
                        AlphaCompositionMode = PixelAlphaCompositionMode.DestIn,
                    },
                };

                i.Fill(opts, Color.Black, ellipsePath);
            });

        return cloned;
    }

    public static Image CropToSquare(Image image)
    {
        int newSize = Math.Min(image.Width, image.Height);

        image.Mutate(i => { i.Crop(newSize, newSize); });

        return image;
    }

    /// <summary>
    ///     PetPet effect.
    ///     Algorithm and sprite sheet source: https://benisland.neocities.org/petpet/
    /// </summary>
    /// <param name="inputImage"> The input image. </param>
    /// <param name="fps">The animation speed in frames per second.</param>
    /// <returns>An animated gif.</returns>
    public static Image PetPet(Image<Rgba32> inputImage, int fps = default)
    {
        const int frameCount = 5;
        const int frameSize = 112;
        const float scale = 0.85f;
        const int overlayY = 0;
        const int alphaThreshold = 120;

        // TODO implement squishFactor
        const float squishFactor = 1f;

        // The maximum fps of 50 results in a delay of 20ms between frames
        // which is the minimum delay for a gif in most renderers.
        const int maxFps = 50;
        const int minFps = 1;
        const int defaultFps = 16;

        int animationFps = fps != default ? Math.Clamp(fps, minFps, maxFps) : defaultFps;

        // Convert the fps value to a delay value represented as the number of frames in hundredths (1/100) of a second
        var animationDelay = (int)Math.Round(100f / animationFps);

        string overlayFile = Path.Combine("Resources", "pet_sprite_sheet.png");
        using Image<Rgba32> overlaySpriteSheet = Image.Load<Rgba32>(overlayFile);

        int overlayWidth = overlaySpriteSheet.Width / frameCount;
        int overlayHeight = overlaySpriteSheet.Height;

        var frames = new Image<Rgba32>[frameCount];
        float[] squishFactors = [0.9f, 0.8f, 0.75f, 0.8f, 0.85f];

        // Resize the input image to match the frame size
        Image<Rgba32> resizedImage = inputImage.Clone(
            ctx =>
            {
                // Crop the image to a square
                ctx.Crop(
                    Math.Min(inputImage.Width, inputImage.Height),
                    Math.Min(inputImage.Width, inputImage.Height));

                // Downscale the image to the frame size and then some
                const int newWidth = (int)(frameSize * scale);
                const int newHeight = (int)(frameSize * scale);

                ctx.Resize(
                    new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Sampler = KnownResamplers.Hermite,
                    });
            });

        // Optimize transparency by clipping pixels with low alpha values in order to reduce dithering artifacts
        resizedImage.ProcessPixelRows(a => ImageProcessors.ClipTransparencyProcessor(a, alphaThreshold));

        for (var i = 0; i < frameCount; i++)
        {
            var overlayFrameRectangle = new Rectangle(i * overlayWidth, 0, overlayWidth, overlayHeight);

            Image<Rgba32> overlayFrame = overlaySpriteSheet.Clone(ctx => ctx.Crop(overlayFrameRectangle));

            float frameSquishFactor = squishFactors[i];

            // Squish the frame
            Image<Rgba32> squishedFrame =
                resizedImage.Clone(x => x.Resize(resizedImage.Width, (int)(resizedImage.Height * frameSquishFactor)));
#if DEBUG
            // temporarily save the frame to disk
            squishedFrame.SaveAsPng(Path.Combine(Path.GetTempPath(), "kattbot", $"a_squished_frame_{i}.png"));
#endif
            var outputFrame = new Image<Rgba32>(frameSize, frameSize);

            // Draw the resized input frame in the bottom right corner
            outputFrame.Mutate(
                x =>
                {
                    int newFrameOffsetX = frameSize - squishedFrame.Width;
                    int newFrameOffsetY = frameSize - squishedFrame.Height;
                    x.DrawImage(squishedFrame, new Point(newFrameOffsetX, newFrameOffsetY), 1f);
                });

            // Draw the overlay frame
            outputFrame.Mutate(x => x.DrawImage(overlayFrame, new Point(0, overlayY), 1f));
#if DEBUG
            // temporarily save the frame to disk
            outputFrame.SaveAsPng(Path.Combine(Path.GetTempPath(), "kattbot", $"b_frame_{i}.png"));
#endif
            frames[i] = outputFrame;
        }

        var outputGif = new Image<Rgba32>(frameSize, frameSize);

        for (var i = 0; i < frameCount; i++)
        {
            Image<Rgba32> currFrame = frames[i].Clone();

            ImageFrame<Rgba32> currRootFrame = currFrame.Frames.RootFrame;

            GifFrameMetadata gifFrameMetadata = currRootFrame.Metadata.GetGifMetadata();
            gifFrameMetadata.FrameDelay = animationDelay;
            gifFrameMetadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;

            outputGif.Frames.AddFrame(currRootFrame);
        }

        // Remove the default frame
        outputGif.Frames.RemoveFrame(0);

        GifMetadata gifMetadata = outputGif.Metadata.GetGifMetadata();
        gifMetadata.RepeatCount = 0;
        gifMetadata.ColorTableMode = GifColorTableMode.Global;

        return outputGif;
    }
}
