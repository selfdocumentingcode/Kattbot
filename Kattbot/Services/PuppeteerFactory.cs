using System.Threading.Tasks;
using PuppeteerSharp;

namespace Kattbot.Services;

public class PuppeteerFactory
{
    public async Task<IBrowser> BuildBrowser()
    {
        var browserFetcher = new BrowserFetcher();

        _ = await browserFetcher.DownloadAsync();

        var options = new LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox"],
        };

        IBrowser browser = await Puppeteer.LaunchAsync(options);
        return browser;
    }
}
