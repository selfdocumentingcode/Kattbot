using System.IO;
using System.Threading.Tasks;
using Kattbot.Services;
using Kattbot.Services.Images;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Kattbot.Tests;

[TestClass]
[Ignore]
public class PetTests
{
    [TestMethod]
    public async Task PetPetTest()
    {
        var puppeteerFactory = new PuppeteerFactory();

        var logger = new Mock<ILogger<PetPetClient>>();

        var makeEmojiClient = new PetPetClient(puppeteerFactory, logger.Object);

        string inputFile = Path.Combine(Path.GetTempPath(), "froge.png");
        string ouputFile = Path.Combine(Path.GetTempPath(), "pet_froge.gif");

        byte[] resultBytes = await makeEmojiClient.PetPet(inputFile);

        using var image = Image.Load(resultBytes);

        await image.SaveAsGifAsync(ouputFile);
    }

    [TestMethod]
    public async Task CropToCircle()
    {
        string inputFile = Path.Combine(Path.GetTempPath(), "froge.png");
        string ouputFile = Path.Combine(Path.GetTempPath(), "froge_circle.png");

        var imageService = new ImageService(null!);

        using var image = Image.Load(inputFile, out IImageFormat? format);

        var imageResult = new ImageResult(image, format);

        ImageResult croppedImageResult = imageService.CropImageToCircle(imageResult);

        await croppedImageResult.Image.SaveAsPngAsync(ouputFile);
    }
}
