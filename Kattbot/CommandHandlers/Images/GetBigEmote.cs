using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers.Images
{
    public class GetBigEmote
    {
        public class GetBigEmoteRequest : CommandRequest
        {
            public DiscordEmoji Emoji { get; set; } = null!;
            public uint? ScaleFactor { get; set; }

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

                string url = GetEmojiImageUrl(emoji);

                if (hasScaleFactor)
                {
                    var scaleFactor = request.ScaleFactor!.Value;

                    var imageBytes = await _imageService.DownloadImageToBytes(url);

                    var doubledImageResult = await _imageService.ScaleImage(imageBytes, scaleFactor);

                    var imageStream = doubledImageResult.Item1;
                    var fileExtension = doubledImageResult.Item2;

                    var responseBuilder = new DiscordMessageBuilder();

                    responseBuilder
                        .WithFile($"bigger.{fileExtension}", imageStream);

                    await ctx.RespondAsync(responseBuilder);
                }
                else
                {
                    await ctx.RespondAsync(url);
                }
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
