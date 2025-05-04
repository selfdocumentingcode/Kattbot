using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kattbot.Services.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Kattbot.Tests;

[TestClass]
[Ignore] // Can't save to /tmp on GitHub Actions. TODO: fix
public class ImageServiceTests
{
    [DataTestMethod]
    [DataRow("cute_cat.jpg")]
    [DataRow("froge.png")]
    public async Task EnsureMaxSize_DownscalesImageIfNeeded(string inputFilename)
    {
        const double maxSizeMb = 1;

        string inputFile = Path.Combine("Resources", inputFilename);

        Image inputImage = await Image.LoadAsync(inputFile);

        Image resizedImage = await ImageService.EnsureMaxImageFileSize(inputImage, maxSizeMb);

        double resizedImageSize = await ImageService.GetImageSizeInMb(resizedImage);

        Assert.IsTrue(resizedImageSize <= maxSizeMb);
    }

    [DataTestMethod]
    [DataRow("slowpoke.jpg", "jpg")]
    [DataRow("slowpoke.png", "png")]
    [DataRow("slowpoke.webp", "webp")]
    [DataRow("slowpoke.gif", "png")]
    [DataRow("slowpoke.bmp", "png")]
    public async Task EnsureSupportedImageFormatOrPng_ConvertsToPngIfFormatIsNotSupported(
        string inputFilename,
        string expectedFileType)
    {
        var supportedFileTypes = new[] { "jpg", "png", "webp" };

        string inputFile = Path.Combine("Resources", inputFilename);

        Image inputImage = await Image.LoadAsync(inputFile);

        Image convertedImage = await ImageService.EnsureSupportedImageFormatOrPng(inputImage, supportedFileTypes);

        string convertedImageFileExt = convertedImage.Metadata.GetFormatOrDefault().FileExtensions.First();

        Assert.Contains(convertedImageFileExt, supportedFileTypes);
    }
}
