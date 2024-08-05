using System.IO;
using System.Threading.Tasks;
using Kattbot.Services.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.Tests;

[TestClass]
[Ignore] // TODO: Fix tests failing when running on GitHub Actions/docker container
public class ImageTests
{
    [DataTestMethod]
    [DataRow("froge.png")]
    public async Task PetPetTest(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image result = ImageEffects.PetPet(image);

        Assert.IsNotNull(result);

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

        Assert.IsNotNull(croppedImage);

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

        Assert.IsNotNull(croppedImage);

        await croppedImage.SaveAsPngAsync(outputFile);

        Assert.IsTrue(File.Exists(outputFile));
    }
}
