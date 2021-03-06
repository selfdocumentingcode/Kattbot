using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Services;
using MediatR;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers.Images
{
    public class GetBigEmote
    {
        public static string EffectDeepFry = "deepfry";
        public static string EffectOilPaint = "oilpaint";

        public class GetBigEmoteRequest : CommandRequest
        {
            public DiscordEmoji Emoji { get; set; } = null!;
            public uint? ScaleFactor { get; set; }
            public string? Effect { get; set; }


            public GetBigEmoteRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class GetBigEmoteHandler : AsyncRequestHandler<GetBigEmoteRequest>
        {
            private readonly ImageService _imageService;

            public GetBigEmoteHandler(ImageService imageService)
            {
                _imageService = imageService;
            }

            protected override async Task Handle(GetBigEmoteRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var emoji = request.Emoji;
                var hasScaleFactor = request.ScaleFactor.HasValue;
                var effect = request.Effect;

                string url = GetEmojiImageUrl(emoji);

                byte[] imageBytes;

                try
                {
                    imageBytes = await _imageService.DownloadImageToBytes(url);
                }
                catch (HttpRequestException)
                {
                    throw new Exception("Couldn't download image");
                }

                MutateImageResult imageResult;

                if (effect == EffectDeepFry)
                {
                    var scaleFactor = request.ScaleFactor.HasValue ? request.ScaleFactor.Value : 2;
                    imageResult = await _imageService.DeepFryImage(imageBytes, scaleFactor);
                }
                else if (effect == EffectOilPaint)
                {
                    var scaleFactor = request.ScaleFactor.HasValue ? request.ScaleFactor.Value : 2;
                    imageResult = await _imageService.OilPaintImage(imageBytes, scaleFactor);
                }
                else
                {
                    if (hasScaleFactor)
                    {
                        imageResult = await _imageService.ScaleImage(imageBytes, request.ScaleFactor!.Value);
                    }
                    else
                    {
                        imageResult = await _imageService.GetImageStream(imageBytes);
                    }
                }

                var imageStream = imageResult.MemoryStream;
                var fileExtension = imageResult.FileExtension;

                var responseBuilder = new DiscordMessageBuilder();

                var fileName = hasScaleFactor ? "bigger" : "big";

                responseBuilder
                    .WithFile($"{fileName}.{fileExtension}", imageStream);

                await ctx.RespondAsync(responseBuilder);
            }

            private string GetEmojiImageUrl(DiscordEmoji emoji)
            {
                var isEmote = emoji.Id != 0;

                return isEmote ? emoji.Url : GetExternalEmojiImageUrl(emoji.Name);
            }

            private string GetExternalEmojiImageUrl(string code)
            {
                // eggplant = 0001F346
                // https://emoji.aranja.com/static/emoji-data/img-twitter-72/1f346.png

                // flag =  0001F1E6 0001F1E9
                // https://emoji.aranja.com/static/emoji-data/img-twitter-72/1f1e6-1f1e9.png

                var utf32Encoding = new UTF32Encoding(true, false);

                var bytes = utf32Encoding.GetBytes(code);

                var sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.AppendFormat("{0:X2}", bytes[i]);
                }

                var bytesAsString = sb.ToString();

                var fileNameBuilder = new StringBuilder();

                for (int i = 0; i < bytesAsString.Length; i += 8)
                {
                    var unicodePart = bytesAsString.Substring(i, 8)
                        .TrimStart('0')
                        .ToLower();

                    fileNameBuilder.Append(i == 0 ? unicodePart : $"-{unicodePart}");
                }

                var fileName = fileNameBuilder.ToString();

                return $"https://emoji.aranja.com/static/emoji-data/img-twitter-72/{fileName}.png";
            }
        }
    }
}
