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
    public async Task PetPetTest()
    {
        var puppeteerFactory = new PuppeteerFactory();

        var logger = Substitute.For<ILogger<PetPetClient>>();

        var makeEmojiClient = new PetPetClient(puppeteerFactory, logger);

        string inputFile = Path.Combine(Path.GetTempPath(), "froge.png");
        string ouputFile = Path.Combine(Path.GetTempPath(), "pet_froge.gif");

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
