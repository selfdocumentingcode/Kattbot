using System;
using System.Collections.Generic;
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
        image.Mutate(i =>
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

        Image<TPixel> cloned = imageAsPngWithTransparency.Clone(i =>
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
        Image<Rgba32> resizedImage = inputImage.Clone(ctx =>
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
            var overlayFrameRectangle = new Rectangle(i * overlayWidth, y: 0, overlayWidth, overlayHeight);

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
            outputFrame.Mutate(x =>
            {
                int newFrameOffsetX = frameSize - squishedFrame.Width;
                int newFrameOffsetY = frameSize - squishedFrame.Height;
                x.DrawImage(squishedFrame, new Point(newFrameOffsetX, newFrameOffsetY), opacity: 1f);
            });

            // Draw the overlay frame
            outputFrame.Mutate(x => x.DrawImage(overlayFrame, new Point(x: 0, overlayY), opacity: 1f));
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

    public static Image FillMaskWithTiledImage(
        Image<Rgba32> targetImage,
        Image<Rgba32> maskImage,
        Image<Rgba32> tileImage)
    {
        Image<Rgba32> result = targetImage.Clone();
        var random = new Random();

        // Calculate average color of tile image for background fill
        Rgba32 averageColor = CalculateAverageColor(tileImage);

        // Apply brightness reduction to the average color for Layer 1
        const float baseLayerBrightness = 0.8f;
        var darkenedAverageColor = new Rgba32(
            (byte)(averageColor.R * baseLayerBrightness),
            (byte)(averageColor.G * baseLayerBrightness),
            (byte)(averageColor.B * baseLayerBrightness),
            averageColor.A);

        // Layer 1: Fill entire masked area with darkened average color using pixel-level processing
        for (var y = 0; y < Math.Min(result.Height, maskImage.Height); y++)
        {
            for (var x = 0; x < Math.Min(result.Width, maskImage.Width); x++)
            {
                Rgba32 maskPixel = maskImage[x, y];
                if (maskPixel is { R: > 200, G: > 200, B: > 200 })
                {
                    result[x, y] = darkenedAverageColor;
                }
            }
        }

        // Layer 2: Texture-like coverage with reduced brightness (depth effect)
        const double textureLayerTileRatio = 1.0 / 34.0;
        const float textureLayerBrightness = 0.9f;
        const float textureLayerSpacingMultiplier = 0.3f;

        ApplyTextureLayer(
            result,
            maskImage,
            tileImage,
            random,
            textureLayerTileRatio,
            textureLayerBrightness,
            textureLayerSpacingMultiplier);

        // Layer 3: Higher tile ratio with original brightness and increased spacing
        const double tileLayerTileRatio = 1.0 / 30.0;
        const float tileLayerBrightness = 1.0f;
        const float tileLayerSpacingMultiplier = 0.65f;

        ApplyTileLayer(
            result,
            maskImage,
            tileImage,
            random,
            tileLayerTileRatio,
            tileLayerBrightness,
            tileLayerSpacingMultiplier);

        return result;
    }

    private static void ApplyTextureLayer(
        Image<Rgba32> result,
        Image<Rgba32> maskImage,
        Image<Rgba32> tileImage,
        Random random,
        double tileRatio,
        float brightness,
        float spacingMultiplier)
    {
        var desiredTileSize = (int)(result.Width * tileRatio);

        // Resize the tile image to maintain square aspect ratio
        using Image<Rgba32> resizedTileImage = tileImage.Clone(ctx =>
            ctx.Resize(desiredTileSize, desiredTileSize, KnownResamplers.Hermite));

        int tileWidth = resizedTileImage.Width;
        int tileHeight = resizedTileImage.Height;
        int maxJitterX = tileWidth / 4; // Reduced jitter for more consistent coverage
        int maxJitterY = tileHeight / 4;

        // Create a grid that ensures complete coverage with some overlap
        var gridSpacingX = (int)(tileWidth * spacingMultiplier);
        var gridSpacingY = (int)(tileHeight * spacingMultiplier);

        // Start with slight negative offset to ensure edge coverage
        int startX = -tileWidth / 2;
        int startY = -tileHeight / 2;

        for (int gridY = startY; gridY < maskImage.Height + tileHeight; gridY += gridSpacingY)
        {
            for (int gridX = startX; gridX < maskImage.Width + tileWidth; gridX += gridSpacingX)
            {
                // Add small random jitter to avoid perfect grid pattern
                int jitterX = random.Next(-maxJitterX, maxJitterX);
                int jitterY = random.Next(-maxJitterY, maxJitterY);

                int tileX = gridX + jitterX;
                int tileY = gridY + jitterY;

                // Check if this tile position overlaps with the mask
                var overlapsMask = false;
                int checkRadius = Math.Max(tileWidth, tileHeight) / 2;

                for (int checkY = Math.Max(val1: 0, tileY);
                     checkY < Math.Min(maskImage.Height, tileY + tileHeight) && !overlapsMask;
                     checkY += checkRadius / 2)
                {
                    for (int checkX = Math.Max(val1: 0, tileX);
                         checkX < Math.Min(maskImage.Width, tileX + tileWidth) && !overlapsMask;
                         checkX += checkRadius / 2)
                    {
                        if (checkX < maskImage.Width && checkY < maskImage.Height)
                        {
                            Rgba32 pixel = maskImage[checkX, checkY];
                            if (pixel is { R: > 200, G: > 200, B: > 200 })
                            {
                                overlapsMask = true;
                            }
                        }
                    }
                }

                if (overlapsMask)
                {
                    // Draw the tile image cropped to the mask boundaries using direct pixel access
                    for (var y = 0; y < tileHeight; y++)
                    {
                        int resultY = tileY + y;
                        if (resultY < 0 || resultY >= result.Height) continue;

                        for (var x = 0; x < tileWidth; x++)
                        {
                            int resultX = tileX + x;
                            if (resultX < 0 || resultX >= result.Width) continue;

                            // Check if this position is within the mask
                            if (resultX < maskImage.Width && resultY < maskImage.Height)
                            {
                                Rgba32 maskPixel = maskImage[resultX, resultY];
                                if (maskPixel is { R: > 200, G: > 200, B: > 200 })
                                {
                                    // Draw tile pixels within the mask, including semi-transparent ones for smooth edges
                                    Rgba32 tilePixel = resizedTileImage[x, y];
                                    if (tilePixel.A > 0) // Include all non-fully-transparent pixels
                                    {
                                        // Apply brightness reduction per-pixel to preserve edge anti-aliasing
                                        var adjustedPixel = new Rgba32(
                                            (byte)(tilePixel.R * brightness),
                                            (byte)(tilePixel.G * brightness),
                                            (byte)(tilePixel.B * brightness),
                                            tilePixel.A); // Keep original alpha

                                        // Blend with existing pixel using alpha blending
                                        Rgba32 existingPixel = result[resultX, resultY];
                                        float alpha = adjustedPixel.A / 255f;
                                        float invAlpha = 1f - alpha;

                                        var blendedPixel = new Rgba32(
                                            (byte)((adjustedPixel.R * alpha) + (existingPixel.R * invAlpha)),
                                            (byte)((adjustedPixel.G * alpha) + (existingPixel.G * invAlpha)),
                                            (byte)((adjustedPixel.B * alpha) + (existingPixel.B * invAlpha)),
                                            Math.Max(adjustedPixel.A, existingPixel.A));

                                        result[resultX, resultY] = blendedPixel;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void ApplyTileLayer(
        Image<Rgba32> result,
        Image<Rgba32> maskImage,
        Image<Rgba32> tileImage,
        Random random,
        double tileRatio,
        float brightness,
        float spacingMultiplier)
    {
        var desiredTileSize = (int)(result.Width * tileRatio);

        // Resize the tile image to maintain square aspect ratio
        using Image<Rgba32> resizedTileImage = tileImage.Clone(ctx =>
            ctx.Resize(desiredTileSize, desiredTileSize, KnownResamplers.Hermite));

        // Adjust brightness if needed
        if (Math.Abs(brightness - 1.0f) > 0.001f)
        {
            resizedTileImage.Mutate(ctx => ctx.Brightness(brightness));
        }

        // Calculate tile placement parameters
        int tileWidth = resizedTileImage.Width;
        int tileHeight = resizedTileImage.Height;
        int maxJitterX = tileWidth / 4;
        int maxJitterY = tileHeight / 4;

        // Create a list of mask regions to fill
        var maskRegions = new List<Point>();

        // Scan for white pixels in the mask (sampling at intervals)
        var spacingY = (int)(tileHeight * spacingMultiplier);
        var spacingX = (int)(tileWidth * spacingMultiplier);

        for (var y = 0; y < maskImage.Height; y += spacingY)
        {
            for (var x = 0; x < maskImage.Width; x += spacingX)
            {
                if (x < maskImage.Width && y < maskImage.Height)
                {
                    Rgba32 pixel = maskImage[x, y];

                    // Check if pixel is white (high RGB values)
                    if (pixel is { R: > 200, G: > 200, B: > 200 })
                    {
                        maskRegions.Add(new Point(x, y));
                    }
                }
            }
        }

        // Place tiles randomly over mask regions
        foreach (Point region in maskRegions)
        {
            // Add random jitter to tile placement
            int jitterX = random.Next(-maxJitterX, maxJitterX);
            int jitterY = random.Next(-maxJitterY, maxJitterY);

            // Center the tile on the detected mask pixel
            int tileX = (region.X + jitterX) - (tileWidth / 2);
            int tileY = (region.Y + jitterY) - (tileHeight / 2);

            // Only draw if the tile center would be within the mask area
            int centerX = tileX + (tileWidth / 2);
            int centerY = tileY + (tileHeight / 2);

            if (centerX >= 0 && centerX < maskImage.Width &&
                centerY >= 0 && centerY < maskImage.Height)
            {
                Rgba32 centerPixel = maskImage[centerX, centerY];
                if (centerPixel is { R: > 200, G: > 200, B: > 200 })
                {
                    // Allow tiles to extend beyond image bounds for natural edge effect
                    // Draw the tile image at the calculated position (can go partially offscreen)
                    result.Mutate(ctx => { ctx.DrawImage(resizedTileImage, new Point(tileX, tileY), opacity: 1f); });
                }
            }
        }
    }

    private static Rgba32 CalculateAverageColor(Image<Rgba32> image)
    {
        long totalR = 0, totalG = 0, totalB = 0, totalA = 0;
        var pixelCount = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                for (var x = 0; x < pixelRow.Length; x++)
                {
                    Rgba32 pixel = pixelRow[x];
                    if (pixel.A > 0) // Only count non-transparent pixels
                    {
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;
                        totalA += pixel.A;
                        pixelCount++;
                    }
                }
            }
        });

        if (pixelCount == 0)
            return new Rgba32(r: 0, g: 0, b: 0, a: 0);

        return new Rgba32(
            (byte)(totalR / pixelCount),
            (byte)(totalG / pixelCount),
            (byte)(totalB / pixelCount),
            (byte)(totalA / pixelCount));
    }
}
