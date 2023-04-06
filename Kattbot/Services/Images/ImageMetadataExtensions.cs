using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;

namespace Kattbot.Services.Images;

public static class ImageMetadataExtensions
{
    public static IImageFormat GetFormatOrDefault(this ImageMetadata metadata) => metadata.DecodedImageFormat ?? PngFormat.Instance;
}
