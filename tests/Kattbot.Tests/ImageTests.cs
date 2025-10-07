using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kattbot.Services.Images;
using Kattbot.Tests.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.Tests;

[TestClass]
[TestCategory("ShittyTests")]
public class ImageTests
{
    private readonly TestContext _testContext;

    public ImageTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    [DataRow("froge.png")]
    public async Task PetPetTest(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image result = ImageEffects.PetPet(image);

        string outputFile = Path.Combine(PathUtils.TryGetTempPathFromEnv(), "kattbot", "z_output_petpet_froge.gif");

        await result.SaveAsGifAsync(outputFile, _testContext.CancellationTokenSource.Token);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DataRow("froge.png")]
    public async Task CropToCircle(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);
        string outputFile = Path.Combine(PathUtils.TryGetTempPathFromEnv(), "kattbot", $"cropped_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image<Rgba32> croppedImage = ImageEffects.CropToCircle(image);

        await croppedImage.SaveAsPngAsync(outputFile, _testContext.CancellationTokenSource.Token);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DataRow("froge.png")]
    public async Task Twirl(string inputFilename)
    {
        string inputFile = Path.Combine("Resources", inputFilename);
        string outputFile = Path.Combine(PathUtils.TryGetTempPathFromEnv(), "kattbot", $"twirled_{inputFilename}");

        using Image<Rgba32> image = Image.Load<Rgba32>(inputFile);

        Image croppedImage = ImageEffects.TwirlImage(image);

        await croppedImage.SaveAsPngAsync(outputFile, _testContext.CancellationTokenSource.Token);

        Assert.IsTrue(File.Exists(outputFile));
    }

    [TestMethod]
    [DynamicData(nameof(GetTileImageFiles), DynamicDataSourceType.Method)]
    public async Task FillMaskWithTiledImage(string tileImageFilename)
    {
        string targetImageFile = Path.Combine("Resources", "dumptruck_v1.png");
        string maskImageFile = Path.Combine("Resources", "dumptruck_v1_double_mask.png");
        string tileImageFile = Path.Combine("Resources", "DumpTruckTiles", tileImageFilename);
        string outputDir = Path.Combine(PathUtils.TryGetTempPathFromEnv(), "kattbot");
        string outputFile = Path.Combine(outputDir, $"mask_filled_{tileImageFilename}");

        Directory.CreateDirectory(outputDir);

        using Image<Rgba32> targetImage = Image.Load<Rgba32>(targetImageFile);
        using Image<Rgba32> maskImage = Image.Load<Rgba32>(maskImageFile);
        using Image<Rgba32> tileImage = Image.Load<Rgba32>(tileImageFile);

        Image filledImage = FillMaskWithTilesEffect.ApplyEffect(targetImage, maskImage, tileImage);

        await filledImage.SaveAsPngAsync(outputFile, _testContext.CancellationTokenSource.Token);

        Assert.IsTrue(File.Exists(outputFile));
    }

    private static IEnumerable<object[]> GetTileImageFiles()
    {
        string tilesDirectory = Path.Combine("Resources", "DumpTruckTiles");

        if (!Directory.Exists(tilesDirectory))
        {
            yield break;
        }

        string[] imageFiles = Directory.GetFiles(tilesDirectory, "*.png");

        foreach (string filePath in imageFiles)
        {
            yield return [Path.GetFileName(filePath)];
        }
    }
}
