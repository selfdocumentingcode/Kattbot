using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using System;
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
            return ctx.RespondAsync(emoji.Url);
        }
    }

    
}
