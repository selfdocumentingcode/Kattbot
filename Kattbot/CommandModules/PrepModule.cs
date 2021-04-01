using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    [Group("prep")]
    public class PrepModule : BaseCommandModule
    {
        [Command("me")]
        public Task GetRandomPrep(CommandContext ctx)
        {
            var prep = GetRandomPrep();
            var scale = GetRandomScale();

            var message = $"You have rolled the preposition `{prep}`";
            message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

            return ctx.RespondAsync(message);
        }

        [Command("user")]
        public Task GetRandomPrep(CommandContext ctx, DiscordUser user)
        {
            var prep = GetRandomPrep();
            var scale = GetRandomScale();

            var mention = user.Mention;

            var message = $"Here's a preposition for you {mention}: `{prep}`";
            message += $"\r\nOn a scale of 1 to {scale}, how satisfied are you with your preposition?";

            return ctx.RespondAsync(message);
        }

        private string GetRandomPrep()
        {
            var preps = new string[] { "På", "For", "Av", "Til", "Om", "I" };

            var prepIdx = new Random().Next(0, preps.Length);

            return preps[prepIdx];
        }

        private string GetRandomScale()
        {
            var scaleLimit = new string[] { "Helvete", "Kva faen", "Kjempebra" };

            var scaleLimitIdx = new Random().Next(0, scaleLimit.Length);

            return scaleLimit[scaleLimitIdx];
        }
    }
}
