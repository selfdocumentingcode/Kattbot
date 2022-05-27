using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Kattbot.Services
{
    public class ImageService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<byte[]> DownloadImageToBytes(string url)
        {
            var client = _httpClientFactory.CreateClient();

            var imageBytes = await client.GetByteArrayAsync(url);

            return imageBytes;
        }

        public async Task<MutateImageResult> ScaleImage(byte[] sourceImageBytes, uint scaleFactor)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            if (format == null) throw new InvalidOperationException("Invalid image format");

            int newWidth = image.Width * (int)scaleFactor;
            int newHeight = image.Height * (int)scaleFactor;

            image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

            return await GetMutatedImageStream(image, format);
        }

        public async Task<MutateImageResult> DeepFryImage(byte[] sourceImageBytes, uint scaleFactor)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            if (format == null) throw new InvalidOperationException("Invalid image format");

            int newWidth = image.Width * (int)scaleFactor;
            int newHeight = image.Height * (int)scaleFactor;

            image.Mutate(i =>
            {
                i.Resize(newWidth, newHeight, KnownResamplers.Welch);
                i.Contrast(5f); // This works
                i.Brightness(1.5f);// This works
                i.GaussianSharpen(5f);
                i.Saturate(5f);
                //i.Dither(KnownDitherings.StevensonArce); 
            });

            return await GetMutatedImageStream(image, format);
        }

        public async Task<MutateImageResult> OilPaintImage(byte[] sourceImageBytes, uint scaleFactor)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            if (format == null) throw new InvalidOperationException("Invalid image format");

            int newWidth = image.Width * (int)scaleFactor;
            int newHeight = image.Height * (int)scaleFactor;

            int paintLevel = 25;

            image.Mutate(i =>
            {
                i.Resize(newWidth, newHeight, KnownResamplers.Welch);
                i.OilPaint(paintLevel, paintLevel);
            });

            return await GetMutatedImageStream(image, format);
        }

        public async Task<MutateImageResult> GetImageStream(byte[] sourceImageBytes)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            if (format == null) throw new InvalidOperationException("Invalid image format");

            return await GetMutatedImageStream(image, format);
        }

        private async Task<MutateImageResult> GetMutatedImageStream(Image image, IImageFormat format)
        {
            var outputStream = new MemoryStream();

            var extensionName = format.Name.ToLower();

            var encoder = GetImageEncoderByFileType(extensionName);

            await image.SaveAsync(outputStream, encoder);

            await outputStream.FlushAsync();

            outputStream.Position = 0;

            return new MutateImageResult
            {
                MemoryStream = outputStream,
                FileExtension = extensionName
            };
        }

        private IImageEncoder GetImageEncoderByFileType(string fileType)
        {
            return fileType switch
            {
                "jpg" => new JpegEncoder(),
                "jpeg" => new JpegEncoder(),
                "png" => new PngEncoder(),
                "gif" => new GifEncoder() { ColorTableMode = GifColorTableMode.Local },
                _ => throw new ArgumentException($"Unknown filetype: {fileType}"),
            };
        }
    }

    public class MutateImageResult
    {
        public MemoryStream MemoryStream { get; set; } = null!;
        public string FileExtension { get; set; } = null!;
    }
}
