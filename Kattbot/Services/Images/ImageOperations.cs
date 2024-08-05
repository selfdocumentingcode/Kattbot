using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.Services.Images;

public static class ImageProcessors
{
    public static void ClipTransparencyProcessor(PixelAccessor<Rgba32> accessor, int threshold)
    {
        threshold = Math.Clamp(threshold, 0, 255);

        Rgba32 transparent = Color.Transparent;
        for (var y = 0; y < accessor.Height; y++)
        {
            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

            // pixelRow.Length has the same value as accessor.Width,
            // but using pixelRow.Length allows the JIT to optimize away bounds checks:
            for (var x = 0; x < pixelRow.Length; x++)
            {
                // Get a reference to the pixel at position x
                ref Rgba32 pixel = ref pixelRow[x];
                if (pixel.A < threshold)
                {
                    // Overwrite the pixel referenced by 'ref Rgba32 pixel':
                    pixel = transparent;
                }
            }
        }
    }
}
