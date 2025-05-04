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

        Image imageInSupportedFormat =
            await ImageService.EnsureSupportedImageFormatOrPng(image, ["png"]);

        Image resizedImage = await ImageService.EnsureMaxImageFileSize(
            imageInSupportedFormat,
            MaxImageSizeInMb);

        Image squaredImage = ImageEffects.CropToSquare(resizedImage);

        int resultSize = Math.Min(
            maxSize,
            Math.Max(ValidSizes.Reverse().FirstOrDefault(s => squaredImage.Height >= s), ValidSizes[0]));

        ImageStreamResult inputImageStream = await ImageService.GetImageStream(squaredImage);

        var imageVariationRequest = new CreateImageVariationRequest
        {
            Image = inputImageStream.MemoryStream.ToArray(),
            Size = $"{resultSize}x{resultSize}",
            User = userId.ToString(),
        };

        var fileName = $"{Guid.NewGuid()}.png";

        CreateImageResponse response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        ImageResponseUrlData imageResponseUrl = response.Data.First();

        Image imageResult = await _imageService.DownloadImage(imageResponseUrl.Url);

        ImageStreamResult imageStream = await ImageService.GetImageStream(imageResult);

        return imageStream;
    }
}
