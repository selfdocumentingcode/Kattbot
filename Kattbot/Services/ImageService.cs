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

        public async Task<(MemoryStream, string)> ScaleImage(byte[] sourceImageBytes, uint scaleFactor)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            var extensionName = format.Name.ToLower();

            int newWidth = image.Width * (int)scaleFactor;
            int newHeight = image.Height * (int)scaleFactor;

            image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

            var outputStream = new MemoryStream();

            var encoder = GetImageEncoderByFileType(extensionName);

            await image.SaveAsync(outputStream, encoder);

            await outputStream.FlushAsync();

            outputStream.Position = 0;

            return (outputStream, extensionName);
        }

        public async Task<(MemoryStream, string)> DeepfryImage(byte[] sourceImageBytes, uint scaleFactor)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            var extensionName = format.Name.ToLower();

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

            var outputStream = new MemoryStream();

            var encoder = GetImageEncoderByFileType(extensionName);

            await image.SaveAsync(outputStream, encoder);

            await outputStream.FlushAsync();

            outputStream.Position = 0;

            return (outputStream, extensionName);
        }

        public async Task<(MemoryStream, string)> GetImageStream(byte[] sourceImageBytes)
        {
            using var image = Image.Load(sourceImageBytes, out var format);

            var extensionName = format.Name.ToLower();

            var outputStream = new MemoryStream();

            var encoder = GetImageEncoderByFileType(extensionName);

            await image.SaveAsync(outputStream, encoder);

            await outputStream.FlushAsync();

            outputStream.Position = 0;

            return (outputStream, extensionName);
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
}
