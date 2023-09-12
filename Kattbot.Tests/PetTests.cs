using System.IO;
using System.Threading.Tasks;
using Kattbot.Services;
using Kattbot.Services.Images;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SixLabors.ImageSharp;

namespace Kattbot.Tests;

[TestClass]
[Ignore]
public class PetTests
{
    [TestMethod]
    [DataRow("SamplePNGImage_100kbmb.png")]
    [DataRow("SamplePNGImage_500kbmb.png")]
    [DataRow("SamplePNGImage_1mbmb.png")]
    [DataRow("SamplePNGImage_3mbmb.png")]
    [DataRow("SamplePNGImage_10mbmb.png")]
    [DataRow("SamplePNGImage_30mbmb.png")]
    public async Task PetPetTest(string inputImage)
    {
        var puppeteerFactory = new PuppeteerFactory();

        var logger = Substitute.For<ILogger<PetPetClient>>();

        var makeEmojiClient = new PetPetClient(puppeteerFactory, logger);

        string inputFile = Path.Combine(Path.GetTempPath(), "test_images", inputImage);
        string ouputFile = Path.Combine(Path.GetTempPath(), "pet-test-output", $"pet_{inputImage.Split(".")[0]}.gif");

        byte[] resultBytes = await makeEmojiClient.PetPet(inputFile);

        using var image = Image.Load(resultBytes);

        await image.SaveAsGifAsync(ouputFile);
    }

    [DataTestMethod]
    [DataRow("froge.png")]
    [DataRow("test_working.png")]
    [DataRow("test_not_working.png")]
    public async Task CropToCircle(string inputFilename)
    {
        string inputFile = Path.Combine(Path.GetTempPath(), inputFilename);
        string ouputFile = Path.Combine(Path.GetTempPath(), $"cropped_{inputFilename}");

        var imageService = new ImageService(null!);

        using var image = Image.Load(inputFile);

        var croppedImage = imageService.CropImageToCircle(image);

        await croppedImage.SaveAsPngAsync(ouputFile);
    }
}
