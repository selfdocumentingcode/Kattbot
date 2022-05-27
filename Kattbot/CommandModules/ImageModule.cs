using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Workers;
using System.Threading.Tasks;
using static Kattbot.CommandHandlers.Images.GetBigEmote;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    public class ImageModule : BaseCommandModule
    {
        private readonly CommandQueue _commandQueue;

        public ImageModule(CommandQueue commandQueue)
        {
            _commandQueue = commandQueue;
        }

        [Command("big")]
        [Cooldown(5, 10, CooldownBucketType.Global)]
        public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
        {
            var request = new GetBigEmoteRequest(ctx)
            {
                Emoji = emoji
            };

            _commandQueue.Enqueue(request);

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

            _commandQueue.Enqueue(request);

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

            _commandQueue.Enqueue(request);

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

            _commandQueue.Enqueue(request);

            return Task.CompletedTask;
        }
    }
}
