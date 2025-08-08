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
    [DataTestMethod]
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

    [DataTestMethod]
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

    [DataTestMethod]
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
}
