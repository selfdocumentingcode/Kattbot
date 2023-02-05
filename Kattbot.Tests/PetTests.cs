using System;
using System.IO;
using System.Threading.Tasks;
using Kattbot.Services;
using Kattbot.Services.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PuppeteerSharp;
using SixLabors.ImageSharp;

namespace Kattbot.Tests;

[TestClass]
[Ignore]
public class PetTests
{
    [TestMethod]
    public async Task Pet()
    {
        var puppeteerFactory = new PuppeteerFactory();

        const string url = "https://makeemoji.com/";

        string inputFile = Path.Combine(Path.GetTempPath(), "froge.png");
        string ouputFile = Path.Combine(Path.GetTempPath(), "pet_froge.gif");

        using IBrowser browser = await puppeteerFactory.BuildBrowser();
        using IPage page = await browser.NewPageAsync();

        IResponse response = await page.GoToAsync(url);

        IElementHandle fileInput = await page.QuerySelectorAsync("input[aria-label^='Upload'][type='file']");

        await fileInput.UploadFileAsync(inputFile);

        await page.WaitForNetworkIdleAsync();

        const string imageElementAltValue = "The generated pet animated emoji";

        IElementHandle imageElement = await page.QuerySelectorAsync($"img[alt='{imageElementAltValue}']");

        IJSHandle imageElementSrcHandle = await imageElement.GetPropertyAsync("src");
        string imageElementSrcValue = await imageElementSrcHandle.JsonValueAsync<string>();

        string cleanBase64 = imageElementSrcValue[(imageElementSrcValue.IndexOf("base64,") + "base64,".Length)..];

        byte[] imageAsBytes = Convert.FromBase64String(cleanBase64);

        using var image = Image.Load(imageAsBytes, out SixLabors.ImageSharp.Formats.IImageFormat? format);

        await image.SaveAsGifAsync(ouputFile);
    }

    [TestMethod]
    public async Task CropToCircle()
    {
        string inputFile = Path.Combine(Path.GetTempPath(), "froge.png");
        string ouputFile = Path.Combine(Path.GetTempPath(), "froge_circle.png");

        var imageService = new ImageService(null!);

        using var image = Image.Load(inputFile, out SixLabors.ImageSharp.Formats.IImageFormat? format);

        var imageResult = new ImageResult(image, format);

        var croppedImageResult = imageService.CropImageToCircle(imageResult);

        await croppedImageResult.Image.SaveAsPngAsync(ouputFile);
    }
}
