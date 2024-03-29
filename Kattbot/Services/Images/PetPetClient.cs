using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Kattbot.Services.Images;

public class PetPetClient
{
    private const string _url = "https://benisland.neocities.org/petpet/";
    private readonly ILogger<PetPetClient> _logger;

    private readonly PuppeteerFactory _puppeteerFactory;

    public PetPetClient(PuppeteerFactory puppeteerFactory, ILogger<PetPetClient> logger)
    {
        _puppeteerFactory = puppeteerFactory;
        _logger = logger;
    }

    public async Task<byte[]> PetPet(string inputFilePath, string? speed = null)
    {
        string inputFile = Path.Combine(Path.GetTempPath(), inputFilePath);

        using IBrowser browser = await _puppeteerFactory.BuildBrowser();
        using IPage page = await browser.NewPageAsync();

        _logger.LogDebug("Opening page");

        IResponse response = await page.GoToAsync(_url);

        IElementHandle fileInput = await page.QuerySelectorAsync("input#uploadFile");

        _logger.LogDebug("Uploading file");

        await fileInput.UploadFileAsync(inputFile);

        _logger.LogDebug("Setting FPS");

        IElementHandle fpsInput = await page.QuerySelectorAsync("input#fpsVal");

        await fpsInput.EvaluateFunctionAsync("(el) => el.value = ''", fpsInput);

        await fpsInput.TypeAsync(ParseSpeed(speed));

        _logger.LogDebug("Clicking export");

        IElementHandle exportButton = await page.QuerySelectorAsync("button#export");

        await exportButton.ClickAsync();

        var imgQuery = "img#result";

        await page.WaitForExpressionAsync($"!!document.querySelector('{imgQuery}').src");

        var src = await page.EvaluateExpressionAsync<string>($"document.querySelector('{imgQuery}').src");

        var imageBase64 = await page.EvaluateFunctionAsync<string>(
            @"
                async src => {
                    const response = await fetch(src);
                    const buffer = await response.arrayBuffer();
                    const base64 = btoa(new Uint8Array(buffer).reduce((data, byte) => data + String.fromCharCode(byte), ''));
                    return base64;
                }",
            src);

        byte[] buffer = Convert.FromBase64String(imageBase64);

        return buffer;
    }

    private static string ParseSpeed(string? speed = null)
    {
        const int speedSlow = 8;
        const int speedNormal = 16;
        const int speedFast = 32;
        const int speedLightspeed = 60;

        return ((speed ?? string.Empty).ToLower() switch
        {
            "slow" => speedSlow,
            "normal" => speedNormal,
            "fast" => speedFast,
            "lightspeed" => speedLightspeed,
            _ => speedNormal,
        }).ToString();
    }
}
