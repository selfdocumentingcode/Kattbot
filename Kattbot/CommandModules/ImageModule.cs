﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Images;
using Kattbot.Workers;
using System.Threading.Tasks;
using static Kattbot.CommandHandlers.Images.GetBigEmote;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    public class ImageModule : BaseCommandModule
    {
        private readonly CommandBag _commandBag;

        public ImageModule(CommandBag commandBag)
        {
            _commandBag = commandBag;
        }

        [Command("big")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            var request = new GetBigEmoteRequest(ctx)
            {
                Emoji = emoji
            };

            _commandBag.Add(request);

            return Task.CompletedTask;
        }

        [Command("bigger")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public Task BiggerEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            var request = new GetBigEmoteRequest(ctx)
            {
                Emoji = emoji,
                ScaleFactor = 2
            };

            _commandBag.Add(request);

            return Task.CompletedTask;
        }

        [Command("deepfry")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public Task DeepFryEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            var request = new GetBigEmoteRequest(ctx)
            {
                Emoji = emoji,
                ScaleFactor = 2,
                Effect = EffectDeepFry
            };

            _commandBag.Add(request);

            return Task.CompletedTask;
        }

        [Command("oilpaint")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public Task OilPaintEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            var request = new GetBigEmoteRequest(ctx)
            {
                Emoji = emoji,
                ScaleFactor = 2,
                Effect = EffectOilPaint
            };

            _commandBag.Add(request);

            return Task.CompletedTask;
        }

        [Command("dalle")]
        [Cooldown(1, 60, CooldownBucketType.Global)]
        public Task Dalle(CommandContext ctx, [RemainingText] string prompt)
        {
            var request = new DallePromptCommand(ctx, prompt);

            _commandBag.Add(request);

            return Task.CompletedTask;
        }
    }
}
