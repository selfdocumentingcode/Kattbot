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
            imageAsPng = ImageEffects.ScaleImage(imageAsPng, 1 / differenceRatio);
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

    public async Task<ImageStreamResult> GetGifImageStream(Image image)
    {
        var outputStream = new MemoryStream();

        const string extensionName = "gif";

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

    public async Task<ImageStreamResult> TransformImage(
        string imageUrl,
        TransformImageEffect effect,
        ImageTransformDelegate<Rgba32>? preTransform = null)
    {
        Image<Rgba32> inputImage = await DownloadImage<Rgba32>(imageUrl);

        if (preTransform != null)
        {
            inputImage = preTransform(inputImage);
        }

        Image imageResult;

        if (effect == TransformImageEffect.DeepFry)
        {
            imageResult = ImageEffects.DeepFryImage(inputImage);
        }
        else if (effect == TransformImageEffect.OilPaint)
        {
            imageResult = ImageEffects.OilPaintImage(inputImage);
        }
        else if (effect == TransformImageEffect.Twirl)
        {
            imageResult = ImageEffects.TwirlImage(inputImage, 90);
        }
        else
        {
            throw new InvalidOperationException($"Unknown effect: {effect}");
        }

        ImageStreamResult imageStreamResult = await GetImageStream(imageResult);

        return imageStreamResult;
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
