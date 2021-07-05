using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Kattbot.Services;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    public class ImageModule : BaseCommandModule
    {
        private readonly DiscordErrorLogger _discordErrorLogger;

        public ImageModule(DiscordErrorLogger discordErrorLogger)
        {
            _discordErrorLogger = discordErrorLogger;
        }

        [Command("big")]
        public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            string url = GetEmojiImageUrl(emoji);

            return ctx.RespondAsync(url);
        }

        [Command("bigger")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public async Task BiggerEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            try
            {
                string url = GetEmojiImageUrl(emoji);

                var httpClient = new HttpClient();

                var imageBytes = await httpClient.GetByteArrayAsync(url);

                using var image = Image.Load(imageBytes, out var format);

                var extensionName = format;

                int newWidth = image.Width * 2;
                int newHeight = image.Height * 2;

                image.Mutate(i => i.Resize(newWidth, newHeight, KnownResamplers.Hermite));

                var outputStream = new MemoryStream();

                var encoder = GetImageEncoderByFileType(format.Name);

                await image.SaveAsync(outputStream, encoder);

                outputStream.Position = 0;

                await outputStream.FlushAsync();

                var responseBuilder = new DiscordMessageBuilder();

                responseBuilder
                    .WithFile($"bigger.{format.Name.ToLower()}", outputStream);

                await ctx.RespondAsync(responseBuilder);

            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());

                throw new Exception("Couldn't make it bigger");
            }
        }

        private string GetEmojiImageUrl(DiscordEmoji emoji)
        {
            var isEmote = emoji.Id != 0;

            return isEmote ? emoji.Url : GetExternalEmojiImageUrl(emoji.Name);
        }

        private IImageEncoder GetImageEncoderByFileType(string fileType)
        {
            return fileType.ToLower() switch
            {
                "png" => new PngEncoder(),
                "gif" => new GifEncoder() { ColorTableMode = GifColorTableMode.Local },
                _ => throw new ArgumentException($"Unknown filetype: {fileType}"),
            };
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
