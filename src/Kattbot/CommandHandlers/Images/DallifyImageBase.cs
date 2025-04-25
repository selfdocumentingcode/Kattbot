using System;
using System.Linq;
using System.Threading.Tasks;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using SixLabors.ImageSharp;

namespace Kattbot.CommandHandlers.Images;

public abstract class DallifyImageHandlerBase
{
    protected const int Size256 = 256;
    protected const int Size512 = 512;
    protected const int Size1024 = 1024;

    private const int MaxImageSizeInMb = 4;

    private static readonly int[] ValidSizes = [Size256, Size512, Size1024];

    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;

    public DallifyImageHandlerBase(DalleHttpClient dalleHttpClient, ImageService imageService)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
    }

    protected async Task<ImageStreamResult> DallifyImage(string imageUrl, ulong userId, int maxSize)
    {
        Image image = await _imageService.DownloadImage(imageUrl);

        Image imageAsPng = await ImageService.ConvertImageToPng(image, MaxImageSizeInMb);

        Image squaredImage = ImageEffects.CropToSquare(imageAsPng);

        int resultSize = Math.Min(
            maxSize,
            Math.Max(ValidSizes.Reverse().FirstOrDefault(s => squaredImage.Height >= s), ValidSizes[0]));

        var fileName = $"{Guid.NewGuid()}.png";

        ImageStreamResult inputImageStream = await _imageService.GetImageStream(squaredImage);

        var imageVariationRequest = new CreateImageVariationRequest
        {
            Image = inputImageStream.MemoryStream.ToArray(),
            Size = $"{resultSize}x{resultSize}",
            User = userId.ToString(),
        };

        CreateImageResponse response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        ImageResponseUrlData imageResponseUrl = response.Data.First();

        Image imageResult = await _imageService.DownloadImage(imageResponseUrl.Url);

        ImageStreamResult imageStream = await _imageService.GetImageStream(imageResult);

        return imageStream;
    }
}
