using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        var paintLevel = 25;

        image.Mutate(i => { i.OilPaint(paintLevel, paintLevel); });

        return image;
    }

    /// <summary>
    ///     Twirls an image
    ///     Source: jhlabs.com.
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

        Func<int, int, (int X, int Y)> transformFn = (x, y) =>
        {
            int newX = x;
            int newY = x;

            float dx = x - centerX;
            float dy = y - centerY;
            float distance = (dx * dx) + (dy * dy);

            if (distance <= radius2)
            {
                distance = (float)Math.Sqrt(distance);
                float a = (float)Math.Atan2(dy, dx) + ((angleRad * (radius - distance)) / radius);

                newX = (int)Math.Floor(centerX + (distance * (float)Math.Cos(a)));
                newY = (int)Math.Floor(centerY + (distance * (float)Math.Sin(a)));
            }

            return (X: newX, Y: newY);
        };

        for (var x = 0; x < src.Width; x++)
        {
            for (var y = 0; y < src.Height; y++)
            {
                (int X, int Y) trans = transformFn(x, y);
                dest[x, y] = src[trans.X, trans.Y];
            }
        }

        return dest;
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
}
