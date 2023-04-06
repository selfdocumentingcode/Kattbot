using System;
using System.Collections.Generic;
using System.Drawing;
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

    public static ImageResult LoadImage(byte[] sourceImageBytes)
    {
        var image = Image.Load(sourceImageBytes, out IImageFormat? format);

        return format == null ? throw new Exception("Invalid image format") : new ImageResult(image, format);
    }

    public async Task<ImageResult> LoadImage(string url)
    {
        byte[] imageBytes;

        try
        {
            HttpClient client = _httpClientFactory.CreateClient();

            imageBytes = await client.GetByteArrayAsync(url);

            ImageResult imageResult = LoadImage(imageBytes);

            return imageResult;
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

    public Task<ImageStreamResult> ScaleImage(ImageResult imageResult, uint scaleFactor)
    {
        Image image = imageResult.Image;

        int newWidth = image.Width * (int)scaleFactor;
        int newHeight = image.Height * (int)scaleFactor;

        image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

        return GetImageStream(imageResult);
    }

    public Task<ImageStreamResult> DeepFryImage(ImageResult imageResult, uint scaleFactor)
    {
        Image image = imageResult.Image;

        int newWidth = image.Width * (int)scaleFactor;
        int newHeight = image.Height * (int)scaleFactor;

        image.Mutate(i =>
        {
            i.Resize(newWidth, newHeight, KnownResamplers.Welch);
            i.Contrast(5f);
            i.Brightness(1.5f);
            i.GaussianSharpen(5f);
            i.Saturate(5f);
        });

        return GetImageStream(imageResult);
    }

    public Task<ImageStreamResult> OilPaintImage(ImageResult imageResult, uint scaleFactor)
    {
        Image image = imageResult.Image;

        int newWidth = image.Width * (int)scaleFactor;
        int newHeight = image.Height * (int)scaleFactor;

        int paintLevel = 25;

        image.Mutate(i =>
        {
            i.Resize(newWidth, newHeight, KnownResamplers.Welch);
            i.OilPaint(paintLevel, paintLevel);
        });

        return GetImageStream(imageResult);
    }

    public ImageResult CropImageToCircle(ImageResult imageResult)
    {
        Image image = imageResult.Image;

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

        return new ImageResult(cloned, imageResult.Format);
    }

    public async Task<ImageStreamResult> CombineImages(string[] base64Images)
    {
        IEnumerable<byte[]> bytesImages = base64Images.Select(Convert.FromBase64String);

        IImageFormat? format = null;

        var images = bytesImages.Select(x => Image.Load(x, out format)).ToList();

        if (format == null)
        {
            throw new InvalidOperationException("Something went wrong");
        }

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

        ImageStreamResult outputImageStream = await GetImageStream(outputImage, format);

        return outputImageStream;
    }

    public Task<ImageStreamResult> GetImageStream(ImageResult imageResult)
    {
        return GetImageStream(imageResult.Image, imageResult.Format);
    }

    public async Task<string> SaveImageToTempPath(ImageResult imageResult, string filename)
    {
        var image = imageResult.Image;
        var format = imageResult.Format;

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{filename}.{extensionName}");

        await image.SaveAsync(tempFilePath, encoder);

        return tempFilePath;
    }

    private async Task<ImageStreamResult> GetImageStream(Image image, IImageFormat format)
    {
        var outputStream = new MemoryStream();

        string extensionName = format.FileExtensions.First();

        IImageEncoder encoder = GetImageEncoderByFileType(extensionName);

        await image.SaveAsync(outputStream, encoder);

        await outputStream.FlushAsync();

        outputStream.Position = 0;

        return new ImageStreamResult(outputStream, extensionName);
    }

    private IImageEncoder GetImageEncoderByFileType(string fileType)
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

public record ImageResult(Image Image, IImageFormat Format) : IDisposable
{
    public void Dispose()
    {
        Image.Dispose();
    }
}

public record ImageStreamResult(MemoryStream MemoryStream, string FileExtension);
