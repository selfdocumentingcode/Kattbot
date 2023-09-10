using System;
using System.Collections.Generic;
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
using Point = SixLabors.ImageSharp.Point;

namespace Kattbot.Services.Images;

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

    public static async Task<Image> ConvertImageToPng(Image image)
    {
        if (image.Metadata.DecodedImageFormat is PngFormat)
            return image;

        using var pngMemoryStream = new MemoryStream();

        await image.SaveAsPngAsync(pngMemoryStream);

        var convertedImage = await Image.LoadAsync(pngMemoryStream);

        return convertedImage;
    }

    public async Task<Image> DownloadImage(string url)
    {
        byte[] imageBytes;

        try
        {
            HttpClient client = _httpClientFactory.CreateClient();

            imageBytes = await client.GetByteArrayAsync(url);

            var image = Image.Load(imageBytes);

            return image;
        }
        catch (HttpRequestException)
        {
            throw new Exception("Couldn't download image");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Task<ImageStreamResult> ScaleImage(Image image, uint scaleFactor)
    {
        int newWidth = image.Width * (int)scaleFactor;
        int newHeight = image.Height * (int)scaleFactor;

        image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

        return GetImageStream(image);
    }

    public Task<ImageStreamResult> DeepFryImage(Image image)
    {
        image.Mutate(i =>
        {
            i.Contrast(5f);
            i.Brightness(1.5f);
            i.GaussianSharpen(5f);
            i.Saturate(5f);
        });

        return GetImageStream(image);
    }

    public Task<ImageStreamResult> OilPaintImage(Image image)
    {
        int paintLevel = 25;

        image.Mutate(i =>
        {
            i.OilPaint(paintLevel, paintLevel);
        });

        return GetImageStream(image);
    }

    public Image CropImageToCircle(Image image)
    {
        var ellipsePath = new EllipsePolygon(image.Width / 2, image.Height / 2, image.Width, image.Height);

        var cloned = image.Clone(i =>
        {
            i.SetGraphicsOptions(new GraphicsOptions()
            {
                Antialias = true,
                AlphaCompositionMode = PixelAlphaCompositionMode.DestIn,
            });

            i.Fill(Color.Red, ellipsePath);
        });

        return cloned;
    }

    public Task<ImageStreamResult> SquareImage(Image image)
    {
        int newSize = Math.Min(image.Width, image.Height);

        image.Mutate(i =>
        {
            i.Resize(newSize, newSize);
        });

        return GetImageStream(image);
    }

    public async Task<ImageStreamResult> CombineImages(string[] base64Images)
    {
        IEnumerable<byte[]> bytesImages = base64Images.Select(Convert.FromBase64String);

        var images = bytesImages.Select(x => Image.Load(x)).ToList();

        // Assume all images have the same size. If this turns out to not be true,
        // might have to upscale/downscale them to get them to be the same size.
        int imageWidth = images.First().Width;
        int imageHeight = images.First().Height;

        int gridSize = (int)Math.Ceiling(Math.Sqrt(images.Count));

        int canvasWidth = imageWidth * gridSize;
        int canvasHeight = imageHeight * gridSize;

        var outputImage = new Image<Rgba32>(canvasWidth, canvasHeight);

        for (int i = 0; i < images.Count; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;

            Image image = images[i];

            int positionX = imageWidth * x;
            int positionY = imageHeight * y;

            outputImage.Mutate(x => x.DrawImage(image, new Point(positionX, positionY), 1f));
        }

        ImageStreamResult outputImageStream = await GetImageStream(outputImage);

        return outputImageStream;
    }

    public async Task<string> SaveImageToTempPath(Image image, string filename)
    {
        var format = image.Metadata.GetFormatOrDefault();

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{filename}.{extensionName}");

        await image.SaveAsync(tempFilePath, encoder);

        return tempFilePath;
    }

    public async Task<ImageStreamResult> GetImageStream(Image image)
    {
        var outputStream = new MemoryStream();

        var format = image.Metadata.GetFormatOrDefault();

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        await image.SaveAsync(outputStream, encoder);

        await outputStream.FlushAsync();

        outputStream.Position = 0;

        return new ImageStreamResult(outputStream, extensionName);
    }

    public string GetImageFileExtension(Image image)
    {
        var format = image.Metadata.GetFormatOrDefault();

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
            "gif" => new GifEncoder() { ColorTableMode = GifColorTableMode.Local },
            "webp" => new WebpEncoder(),
            _ => throw new ArgumentException($"Unknown filetype: {fileType}"),
        };
    }
}

public record ImageStreamResult(MemoryStream MemoryStream, string FileExtension);
