using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;

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

            outputStream.Position = 0;

            await outputStream.FlushAsync();

            return (outputStream, extensionName);
        }

        private IImageEncoder GetImageEncoderByFileType(string fileType)
        {
            return fileType switch
            {
                "png" => new PngEncoder(),
                "gif" => new GifEncoder() { ColorTableMode = GifColorTableMode.Local },
                _ => throw new ArgumentException($"Unknown filetype: {fileType}"),
            };
        }
    }
}
