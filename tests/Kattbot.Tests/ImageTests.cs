using System.IO;
using System.Threading.Tasks;
using Kattbot.Services.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.Tests;

[TestClass]
[Ignore] // Can't save to /tmp on GitHub Actions. TODO: fix
public class ImageTests
{
    [TestMethod]
    [DataRow("froge.png")]
    public async Task PetPetTest(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image result = ImageEffects.PetPet(image);

        string outputFile = Path.Combine(Path.GetTempPath(), "kattbot", "z_output_petpet_froge.gif");

        await result.SaveAsGifAsync(outputFile);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DataRow("froge.png")]
    public async Task CropToCircle(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);
        string outputFile = Path.Combine(Path.GetTempPath(), "kattbot", $"cropped_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image<Rgba32> croppedImage = ImageEffects.CropToCircle(image);

        await croppedImage.SaveAsPngAsync(outputFile);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DataRow("froge.png")]
    public async Task Twirl(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);
        string outputFile = Path.Combine(Path.GetTempPath(), "kattbot", $"twirled_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image croppedImage = ImageEffects.TwirlImage(image);

        await croppedImage.SaveAsPngAsync(outputFile);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DataRow("froge.png")]
    [DataRow("madjoy.png")]
    public async Task FillMaskWithTiledImage(string tileImageFilename)
    {
        string targetImageFile = Path.Combine("Resources", "dumptruck_v1.png");
        string maskImageFile = Path.Combine("Resources", "dumptruck_v1_mask.png");
        string tileImageFile = Path.Combine("Resources", tileImageFilename);
        string outputDir = Path.Combine(Path.GetTempPath(), "kattbot");
        string outputFile = Path.Combine(outputDir, $"mask_filled_{tileImageFilename}");

        Directory.CreateDirectory(outputDir);

        using Image<Rgba32> targetImage = Image.Load<Rgba32>(targetImageFile);
        using Image<Rgba32> maskImage = Image.Load<Rgba32>(maskImageFile);
        using Image<Rgba32> tileImage = Image.Load<Rgba32>(tileImageFile);

        Image filledImage = ImageEffects.FillMaskWithTiledImage(targetImage, maskImage, tileImage);

        await filledImage.SaveAsPngAsync(outputFile);

        Assert.IsTrue(File.Exists(outputFile));
    }
}
