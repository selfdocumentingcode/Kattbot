using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Kattbot.Services.Images;
public class MakeEmojiClient
{
    private const string _url = "https://makeemoji.com/";

    private readonly PuppeteerFactory _puppeteerFactory;

    public MakeEmojiClient(PuppeteerFactory puppeteerFactory)
    {
        _puppeteerFactory = puppeteerFactory;
    }

    public async Task<byte[]> MakeEmojiPet(string inputFilePath)
    {
        string inputFile = Path.Combine(Path.GetTempPath(), inputFilePath);

        using IBrowser browser = await _puppeteerFactory.BuildBrowser();
        using IPage page = await browser.NewPageAsync();

        IResponse response = await page.GoToAsync(_url);

        IElementHandle fileInput = await page.QuerySelectorAsync("input[aria-label^='Upload'][type='file']");

        await fileInput.UploadFileAsync(inputFile);

        await page.WaitForNetworkIdleAsync();

        const string imageElementAltValue = "The generated pet animated emoji";

        IElementHandle imageElement = await page.QuerySelectorAsync($"img[alt='{imageElementAltValue}']");

        IJSHandle imageElementSrcHandle = await imageElement.GetPropertyAsync("src");
        string imageElementSrcValue = await imageElementSrcHandle.JsonValueAsync<string>();

        string cleanBase64 = imageElementSrcValue[(imageElementSrcValue.IndexOf("base64,") + "base64,".Length)..];

        byte[] imageAsBytes = Convert.FromBase64String(cleanBase64);

        return imageAsBytes;
    }
}
