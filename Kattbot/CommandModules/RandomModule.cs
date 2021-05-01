using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    public class RandomModule : BaseCommandModule
    {
        [Command("meow")]
        public Task Meow(CommandContext ctx)
        {
            var result = new Random().Next(0, 10);

            if (result == 0)
                return ctx.RespondAsync($"Woof!\r\nOops.. I mean... meow? :grimacing:");

            return ctx.RespondAsync("Meow!");
        }

        [Command("mjau")]
        public Task Mjau(CommandContext ctx)
        {
            var result = new Random().Next(0, 10);

            if (result == 0)
                return ctx.RespondAsync("Voff!\r\nOi.. Fytti katta... mjau? :grimacing:");

            return ctx.RespondAsync("Mjau!");
        }

        [Command("big")]
        public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            string url;

            if (emoji.Id != 0)
            {
                url = emoji.Url;
            }
            else
            {
                url = GetExternalEmojiImageUrl(emoji.Name);
            }

            return ctx.RespondAsync(url);
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
