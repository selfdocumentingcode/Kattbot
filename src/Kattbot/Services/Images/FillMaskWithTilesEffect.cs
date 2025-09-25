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

public static class FillMaskWithTilesEffect
{
    public static Image ApplyEffect(
        Image<Rgba32> targetImage,
        Image<Rgba32> maskImage,
        Image<Rgba32> tileImage)
    {
        Image<Rgba32> result = targetImage.Clone();
        var random = new Random();

        // Single-pass mask analysis to pre-compute green and red regions
        (HashSet<Point> greenRegions, HashSet<Point> redRegions) = AnalyzeMask(maskImage);

        if (greenRegions.Count == 0)
            return result; // No green regions to fill

        // Calculate average color of tile image for background fill
        Rgba32 averageColor = CalculateAverageColor(tileImage);

        // Apply brightness reduction to the average color for Layer 1
        const float baseLayerBrightness = 0.8f;
        var darkenedAverageColor = new Rgba32(
            (byte)(averageColor.R * baseLayerBrightness),
            (byte)(averageColor.G * baseLayerBrightness),
            (byte)(averageColor.B * baseLayerBrightness),
            averageColor.A);

        // Layer 1: Fill entire masked area with darkened average color using optimized processing
        FastFillMaskedArea(result, greenRegions, darkenedAverageColor);

        // Layer 2: Texture-like coverage with reduced brightness (depth effect)
        const double textureLayerTileRatio = 1.0 / 34.0;
        const float textureLayerBrightness = 0.9f;
        const float textureLayerSpacingMultiplier = 0.3f;

        ApplyTextureLayer(
            result,
            greenRegions,
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
            greenRegions,
            tileImage,
            random,
            tileLayerTileRatio,
            tileLayerBrightness,
            tileLayerSpacingMultiplier);

        // Layer 4: Clip tiles that intersect with red pixels in the mask
        ApplyRedPixelClipping(result, targetImage, redRegions);

        return result;
    }

    private static (HashSet<Point> GreenRegions, HashSet<Point> RedRegions) AnalyzeMask(Image<Rgba32> maskImage)
    {
        var greenRegions = new HashSet<Point>();
        var redRegions = new HashSet<Point>();

        maskImage.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    Rgba32 pixel = row[x];
                    if (pixel is { G: > 200, R: < 100, B: < 100 })
                        greenRegions.Add(new Point(x, y));
                    else if (pixel is { R: > 200, G: < 100, B: < 100 })
                        redRegions.Add(new Point(x, y));
                }
            }
        });

        return (greenRegions, redRegions);
    }

    private static void FastFillMaskedArea(Image<Rgba32> result, HashSet<Point> greenRegions, Rgba32 fillColor)
    {
        result.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (greenRegions.Contains(new Point(x, y)))
                    {
                        row[x] = fillColor;
                    }
                }
            }
        });
    }

    private static HashSet<Point> CreateSpatialIndex(HashSet<Point> regions, int gridSize)
    {
        var spatialIndex = new HashSet<Point>();
        foreach (Point region in regions)
        {
            spatialIndex.Add(new Point(region.X / gridSize, region.Y / gridSize));
        }

        return spatialIndex;
    }

    private static bool HasOverlapWithSpatialIndex(
        HashSet<Point> spatialIndex,
        int tileX,
        int tileY,
        int tileWidth,
        int tileHeight,
        int gridSize)
    {
        int startGridX = Math.Max(val1: 0, tileX / gridSize);
        int endGridX = (tileX + tileWidth) / gridSize;
        int startGridY = Math.Max(val1: 0, tileY / gridSize);
        int endGridY = (tileY + tileHeight) / gridSize;

        for (int gridY = startGridY; gridY <= endGridY; gridY++)
        {
            for (int gridX = startGridX; gridX <= endGridX; gridX++)
            {
                if (spatialIndex.Contains(new Point(gridX, gridY)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ApplyRedPixelClipping(
        Image<Rgba32> result,
        Image<Rgba32> targetImage,
        HashSet<Point> redRegions)
    {
        result.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (redRegions.Contains(new Point(x, y)))
                    {
                        if (x < targetImage.Width && y < targetImage.Height)
                        {
                            row[x] = targetImage[x, y];
                        }
                    }
                }
            }
        });
    }

    private static void ApplyTextureLayer(
        Image<Rgba32> result,
        HashSet<Point> greenRegions,
        Image<Rgba32> tileImage,
        Random random,
        double tileRatio,
        float brightness,
        float spacingMultiplier)
    {
        var desiredTileSize = (int)(result.Width * tileRatio);

        // Pre-resize the tile image once for this layer
        using Image<Rgba32> resizedTileImage = tileImage.Clone(ctx =>
            ctx.Resize(desiredTileSize, desiredTileSize, KnownResamplers.Hermite));

        int tileWidth = resizedTileImage.Width;
        int tileHeight = resizedTileImage.Height;
        int maxJitterX = tileWidth / 4;
        int maxJitterY = tileHeight / 4;

        // Create spatial index for faster overlap detection
        const int gridSize = 16; // Grid cell size for spatial indexing
        HashSet<Point> spatialIndex = CreateSpatialIndex(greenRegions, gridSize);

        // Create a grid that ensures complete coverage with some overlap
        var gridSpacingX = (int)(tileWidth * spacingMultiplier);
        var gridSpacingY = (int)(tileHeight * spacingMultiplier);

        int startX = -tileWidth / 2;
        int startY = -tileHeight / 2;

        for (int gridY = startY; gridY < result.Height + tileHeight; gridY += gridSpacingY)
        {
            for (int gridX = startX; gridX < result.Width + tileWidth; gridX += gridSpacingX)
            {
                int jitterX = random.Next(-maxJitterX, maxJitterX);
                int jitterY = random.Next(-maxJitterY, maxJitterY);

                int tileX = gridX + jitterX;
                int tileY = gridY + jitterY;

                // Use spatial indexing for fast overlap detection
                if (HasOverlapWithSpatialIndex(spatialIndex, tileX, tileY, tileWidth, tileHeight, gridSize))
                {
                    // Draw the tile using optimized pixel processing
                    DrawMaskedTile(
                        result,
                        resizedTileImage,
                        greenRegions,
                        tileX,
                        tileY,
                        brightness);
                }
            }
        }
    }

    private static void ApplyTileLayer(
        Image<Rgba32> result,
        HashSet<Point> greenRegions,
        Image<Rgba32> tileImage,
        Random random,
        double tileRatio,
        float brightness,
        float spacingMultiplier)
    {
        var desiredTileSize = (int)(result.Width * tileRatio);

        // Pre-resize the tile image once for this layer
        using Image<Rgba32> resizedTileImage = tileImage.Clone(ctx =>
            ctx.Resize(desiredTileSize, desiredTileSize, KnownResamplers.Hermite));

        // Apply brightness adjustment if needed
        if (Math.Abs(brightness - 1.0f) > 0.001f)
        {
            resizedTileImage.Mutate(ctx => ctx.Brightness(brightness));
        }

        int tileWidth = resizedTileImage.Width;
        int tileHeight = resizedTileImage.Height;
        int maxJitterX = tileWidth / 4;
        int maxJitterY = tileHeight / 4;

        // Create a sampled list of green regions for tile placement
        var spacingY = (int)(tileHeight * spacingMultiplier);
        var spacingX = (int)(tileWidth * spacingMultiplier);

        var sampledRegions = new List<Point>();
        foreach (Point region in greenRegions)
        {
            if (region.X % spacingX == 0 && region.Y % spacingY == 0)
            {
                sampledRegions.Add(region);
            }
        }

        // Place tiles at sampled locations
        foreach (Point region in sampledRegions)
        {
            int jitterX = random.Next(-maxJitterX, maxJitterX);
            int jitterY = random.Next(-maxJitterY, maxJitterY);

            int tileX = (region.X + jitterX) - (tileWidth / 2);
            int tileY = (region.Y + jitterY) - (tileHeight / 2);

            int centerX = tileX + (tileWidth / 2);
            int centerY = tileY + (tileHeight / 2);

            if (centerX >= 0 && centerX < result.Width &&
                centerY >= 0 && centerY < result.Height &&
                greenRegions.Contains(new Point(centerX, centerY)))
            {
                // Create drop shadow parameters
                const int shadowOffsetX = -1;
                const int shadowOffsetY = -1;
                const float shadowOpacity = 0.3f;

                // Draw drop shadow first
                result.Mutate(ctx =>
                {
                    // ReSharper disable once AccessToDisposedClosure - It's fiiine
                    using Image<Rgba32> shadowImage = resizedTileImage.Clone(shadowCtx =>
                    {
                        shadowCtx.Brightness(0.2f);
                    });

                    ctx.DrawImage(
                        shadowImage,
                        new Point(tileX + shadowOffsetX, tileY + shadowOffsetY),
                        shadowOpacity);
                });

                // Draw the actual tile
                // ReSharper disable once AccessToDisposedClosure - It's fiiine
                result.Mutate(ctx => { ctx.DrawImage(resizedTileImage, new Point(tileX, tileY), opacity: 1f); });
            }
        }
    }

    private static void DrawMaskedTile(
        Image<Rgba32> result,
        Image<Rgba32> tileImage,
        HashSet<Point> greenRegions,
        int tileX,
        int tileY,
        float brightness)
    {
        int tileWidth = tileImage.Width;
        int tileHeight = tileImage.Height;

        // Process both images simultaneously
        result.ProcessPixelRows(
            tileImage,
            (resultAccessor, tileAccessor) =>
            {
                for (var y = 0; y < tileHeight; y++)
                {
                    int resultY = tileY + y;
                    if (resultY < 0 || resultY >= resultAccessor.Height) continue;

                    Span<Rgba32> resultRow = resultAccessor.GetRowSpan(resultY);
                    Span<Rgba32> tileRow = tileAccessor.GetRowSpan(y);

                    for (var x = 0; x < tileWidth; x++)
                    {
                        int resultX = tileX + x;
                        if (resultX < 0 || resultX >= resultAccessor.Width) continue;

                        // Check if this position is within the green regions
                        if (greenRegions.Contains(new Point(resultX, resultY)))
                        {
                            Rgba32 tilePixel = tileRow[x];
                            if (tilePixel.A > 0)
                            {
                                // Apply brightness reduction
                                var adjustedPixel = new Rgba32(
                                    (byte)(tilePixel.R * brightness),
                                    (byte)(tilePixel.G * brightness),
                                    (byte)(tilePixel.B * brightness),
                                    tilePixel.A);

                                // Alpha blend with existing pixel
                                Rgba32 existingPixel = resultRow[resultX];
                                float alpha = adjustedPixel.A / 255f;
                                float invAlpha = 1f - alpha;

                                var blendedPixel = new Rgba32(
                                    (byte)((adjustedPixel.R * alpha) + (existingPixel.R * invAlpha)),
                                    (byte)((adjustedPixel.G * alpha) + (existingPixel.G * invAlpha)),
                                    (byte)((adjustedPixel.B * alpha) + (existingPixel.B * invAlpha)),
                                    Math.Max(adjustedPixel.A, existingPixel.A));

                                resultRow[resultX] = blendedPixel;
                            }
                        }
                    }
                }
            });
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
                foreach (Rgba32 pixel in pixelRow)
                {
                    // Only count non-transparent pixels
                    if (pixel.A <= 0) continue;

                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    totalA += pixel.A;
                    pixelCount++;
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
