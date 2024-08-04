using System.IO;
using System.Threading.Tasks;
using Kattbot.Services.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.Tests;

[TestClass]
[Ignore]
public class ImageTests
{
    [TestMethod]
    public void PetPetTest(string inputImage)
    {
        Assert.IsTrue(true);
    }

    [DataTestMethod]
    [DataRow("froge.png")]
    [DataRow("test_working.png")]
    [DataRow("test_not_working.png")]
    public async Task CropToCircle(string inputFilename)
    {
        string inputFile = Path.Combine(Path.GetTempPath(), inputFilename);
        string ouputFile = Path.Combine(Path.GetTempPath(), "kattbot", $"cropped_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image<Rgba32> croppedImage = ImageEffects.CropToCircle(image);

        await croppedImage.SaveAsPngAsync(ouputFile);
    }

    [DataTestMethod]
    [DataRow("froge.png")]
    public async Task Twirl(string inputFilename)
    {
        string inputFile = Path.Combine(Path.GetTempPath(), inputFilename);
        string ouputFile = Path.Combine(Path.GetTempPath(), "kattbot", $"twirled_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image croppedImage = ImageEffects.TwirlImage(image);

        await croppedImage.SaveAsPngAsync(ouputFile);
    }
}
