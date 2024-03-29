using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Path = System.IO.Path;

namespace Kattbot.Services.Images;

public delegate Image<TPixel> ImageTransformDelegate<TPixel>(Image<TPixel> input)
    where TPixel : unmanaged, IPixel<TPixel>;

public class ImageService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ImageService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public static Image LoadImage(byte[] imageBytes)
    {
        return Image.Load(imageBytes);
    }

    public async Task<Image> ConvertImageToPng(Image image, int? maxSizeInMb = null)
    {
        using var pngMemoryStream = new MemoryStream();

        await image.SaveAsPngAsync(pngMemoryStream);

        double sizeInMb = (double)pngMemoryStream.Length / (1024 * 1024);

        bool imageLargerThanMaxSize = maxSizeInMb.HasValue && sizeInMb > maxSizeInMb;
        bool imageNotPng = image.Metadata.DecodedImageFormat is not PngFormat;

        if (!imageLargerThanMaxSize && !imageNotPng)
        {
            return image;
        }

        pngMemoryStream.Position = 0;
        Image imageAsPng = await Image.LoadAsync(pngMemoryStream);

        if (imageLargerThanMaxSize)
        {
            double differenceRatio = sizeInMb / (int)maxSizeInMb!;
            imageAsPng = ScaleImage(imageAsPng, 1 / differenceRatio);
        }

        return imageAsPng;
    }

    public async Task<Image> DownloadImage(string url)
    {
        byte[] bytes = await DownloadImageBytes(url);

        return Image.Load(bytes);
    }

    public async Task<Image<TPixel>> DownloadImage<TPixel>(string url)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        byte[] bytes = await DownloadImageBytes(url);

        return Image.Load<TPixel>(bytes);
    }

    private async Task<byte[]> DownloadImageBytes(string url)
    {
        try
        {
            HttpClient client = _httpClientFactory.CreateClient();

            return await client.GetByteArrayAsync(url);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Couldn't download image");
        }
    }

    public Image ScaleImage(Image image, double scaleFactor)
    {
        var newWidth = (int)(image.Width * scaleFactor);
        var newHeight = (int)(image.Height * scaleFactor);

        image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

        return image;
    }

    public Image DeepFryImage(Image image)
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

    public Image OilPaintImage(Image image)
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
    public Image TwirlImage(Image<Rgba32> src, float angleDeg = 180)
    {
        var dest = new Image<Rgba32>(src.Width, src.Height);

        int centerX = src.Width / 2;
        int centerY = src.Height / 2;
        int radius = Math.Min(centerX, centerY);
        int radius2 = radius * radius;
        var angleRad = (float)(angleDeg * Math.PI / 180);

        Func<int, int, (int x, int y)> transformFn = (x, y) =>
        {
            int newX = x;
            int newY = x;

            float dx = x - centerX;
            float dy = y - centerY;
            float distance = dx * dx + dy * dy;

            if (distance <= radius2)
            {
                distance = (float)Math.Sqrt(distance);
                float a = (float)Math.Atan2(dy, dx) + angleRad * (radius - distance) / radius;

                newX = (int)Math.Floor(centerX + distance * (float)Math.Cos(a));
                newY = (int)Math.Floor(centerY + distance * (float)Math.Sin(a));
            }

            return (x: newX, y: newY);
        };

        for (var x = 0; x < src.Width; x++)
        {
            for (var y = 0; y < src.Height; y++)
            {
                (int x, int y) trans = transformFn(x, y);
                dest[x, y] = src[trans.x, trans.y];
            }
        }

        return dest;
    }

    public Image<TPixel> CropToCircle<TPixel>(Image<TPixel> image)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        var ellipsePath = new EllipsePolygon(image.Width / 2, image.Height / 2, image.Width, image.Height);

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

    public Image CropToSquare(Image image)
    {
        int newSize = Math.Min(image.Width, image.Height);

        image.Mutate(i => { i.Crop(newSize, newSize); });

        return image;
    }

    public async Task<string> SaveImageToTempPath(Image image, string filename)
    {
        IImageFormat format = image.Metadata.GetFormatOrDefault();

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        string tempFilePath = Path.Combine(Path.GetTempPath(), $"{filename}.{extensionName}");

        await image.SaveAsync(tempFilePath, encoder);

        return tempFilePath;
    }

    public async Task<ImageStreamResult> GetImageStream(Image image)
    {
        var outputStream = new MemoryStream();

        IImageFormat format = image.Metadata.GetFormatOrDefault();

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        await image.SaveAsync(outputStream, encoder);

        await outputStream.FlushAsync();

        outputStream.Position = 0;

        return new ImageStreamResult(outputStream, extensionName);
    }

    public string GetImageFileExtension(Image image)
    {
        IImageFormat format = image.Metadata.GetFormatOrDefault();

        string extensionName = format.FileExtensions.First();

        return extensionName;
    }

    private static IImageEncoder GetImageEncoderByFileType(string fileType)
    {
        return fileType switch
        {
            "jpg" => new JpegEncoder(),
            "jpeg" => new JpegEncoder(),
            "png" => new PngEncoder(),
            "gif" => new GifEncoder { ColorTableMode = GifColorTableMode.Local },
            "webp" => new WebpEncoder(),
            _ => throw new ArgumentException($"Unknown filetype: {fileType}"),
        };
    }
}

public record ImageStreamResult(MemoryStream MemoryStream, string FileExtension);
