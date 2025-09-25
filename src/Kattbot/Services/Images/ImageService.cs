using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kattbot.CommandHandlers.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

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

    public static async Task<double> GetImageSizeInMb(Image image)
    {
        using var memoryStream = new MemoryStream();

        IImageEncoder encoder = GetImageEncoder(image);

        await image.SaveAsync(memoryStream, encoder);

        double sizeInMb = (double)memoryStream.Length / (1024 * 1024);

        return sizeInMb;
    }

    public static async Task<Image> EnsureMaxImageFileSize(Image image, double maxSizeInMb)
    {
        double sizeInMb = await GetImageSizeInMb(image);

        if (sizeInMb <= maxSizeInMb) return image;

        double differenceRatio = sizeInMb / maxSizeInMb;
        image = ImageEffects.ScaleImage(image, 1 / differenceRatio);

        return image;
    }

    /// <summary>
    ///     Ensures the image file type is one of the supported file types.
    ///     Otherwise, convert the image file to png.
    /// </summary>
    /// <param name="image">The input image.</param>
    /// <param name="supportedFileTypes">List of supported image file types.</param>
    /// <returns>The input image if is of the supported image file type, or the image converted to png.</returns>
    public static async Task<Image> EnsureSupportedImageFormatOrPng(Image image, string[] supportedFileTypes)
    {
        string extensionName = GetImageFileExtension(image);

        if (supportedFileTypes.Contains(extensionName))
        {
            return image;
        }

        using var memoryStream = new MemoryStream();

        await image.SaveAsPngAsync(memoryStream);

        memoryStream.Position = 0;

        Image imageAsPng = await Image.LoadAsync(memoryStream);

        return imageAsPng;
    }

    public static Image ConvertBase64ToImage(string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);

        return Image.Load(bytes);
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

    public static async Task<ImageStreamResult> GetImageStream(Image image)
    {
        var outputStream = new MemoryStream();

        string extensionName = GetImageFileExtension(image);

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        await image.SaveAsync(outputStream, encoder);

        await outputStream.FlushAsync();

        outputStream.Position = 0;

        return new ImageStreamResult(outputStream, extensionName);
    }

    public static async Task<ImageStreamResult> GetGifImageStream(Image image)
    {
        var outputStream = new MemoryStream();

        const string extensionName = "gif";

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        await image.SaveAsync(outputStream, encoder);

        await outputStream.FlushAsync();

        outputStream.Position = 0;

        return new ImageStreamResult(outputStream, extensionName);
    }

    private async Task<byte[]> DownloadImageBytes(string url)
    {
        const long maxImageSizeInBytes = 1024 * 1024 * 1024; // 1GB

        try
        {
            HttpClient client = _httpClientFactory.CreateClient();

            client.MaxResponseContentBufferSize = maxImageSizeInBytes;

            return await client.GetByteArrayAsync(url);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Couldn't download image");
        }
    }

    private static IImageEncoder GetImageEncoder(Image image)
    {
        string extensionName = GetImageFileExtension(image);

        return GetImageEncoderByFileType(extensionName);
    }

    private static string GetImageFileExtension(Image image)
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
            _ => throw new ArgumentException($"Unsupported filetype: {fileType}"),
        };
    }
}

public record ImageStreamResult(MemoryStream MemoryStream, string FileExtension);
