using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandHandlers;
using Kattbot.Helper;
using Kattbot.Helpers;
using Kattbot.Models;
using Kattbot.NotificationHandlers;
using Kattbot.Workers;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    [Group("utils")]
    public class UtilsModule : BaseCommandModule
    {
        private readonly ILogger<UtilsModule> _logger;
        private readonly DiscordClient _client;
        private readonly CommandQueue _commandQueue;
        private readonly CommandParallelQueue _commandParallelQueue;
        private readonly EventQueue _eventQueue;

        public UtilsModule(ILogger<UtilsModule> logger, DiscordClient client, CommandQueue commandQueue, CommandParallelQueue commandParallelQueue, EventQueue eventQueue)
        {
            _logger = logger;
            _client = client;
            _commandQueue = commandQueue;
            _commandParallelQueue = commandParallelQueue;
            _eventQueue = eventQueue;
        }

        [Command("emoji-code")]
        public async Task GetEmojiCode(CommandContext ctx, DiscordEmoji emoji)
        {
            var isUnicodeEmoji = emoji.Id == 0;

            if (isUnicodeEmoji)
            {
                var unicodeEncoding = new UnicodeEncoding(true, false);

                var bytes = unicodeEncoding.GetBytes(emoji.Name);

                var sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.AppendFormat("{0:X2}", bytes[i]);
                }

                var bytesAsString = sb.ToString();

                var formattedSb = new StringBuilder();

                for (int i = 0; i < sb.Length; i += 4)
                {
                    formattedSb.Append($"\\u{bytesAsString.Substring(i, 4)}");
                }

                var result = formattedSb.ToString();

                await ctx.RespondAsync($"`{result}`");
            }
            else
            {
                var result = EmoteHelper.BuildEmoteCode(emoji.Id, emoji.Name, emoji.IsAnimated);

                await ctx.RespondAsync($"`{result}`");
            }
        }

        [Command("role-id")]
        public async Task GetRoleId(CommandContext ctx, string roleName)
        {
            var result = DiscordRoleResolver.TryResolveByName(ctx.Guild, roleName, out var discordRole);

            if (!result.Resolved)
            {
                await ctx.RespondAsync(result.ErrorMessage);
                return;
            }

            await ctx.RespondAsync($"Role {roleName} has id {discordRole.Id}");
        }

        [Command("test-queue-error")]
        public Task TestError(CommandContext ctx)
        {
            _commandQueue.Enqueue(new ErrorTestCommand(ctx, "Error 1", 0));

            _commandQueue.Enqueue(new ErrorTestCommand(ctx, "Error 2", 2000));

            _commandQueue.Enqueue(new ErrorTestCommand(ctx, "Error 3", 0));

            return Task.CompletedTask;
        }

        [Command("test-parallel-error")]
        public Task TestError3(CommandContext ctx)
        {
            _commandParallelQueue.Enqueue(new ErrorTestCommand(ctx, "Error 1", 0));

            _commandParallelQueue.Enqueue(new ErrorTestCommand(ctx, "Error 2", 2000));

            _commandParallelQueue.Enqueue(new ErrorTestCommand(ctx, "Error 3", 0));

            return Task.CompletedTask;
        }

        [Command("test-event-error")]
        public Task TestError2(CommandContext ctx)
        {
            var eventCtx = new EventContext()
            {
                EventName = nameof(TestError2),
                Channel = ctx.Channel,
                Guild = ctx.Guild,
                Message = ctx.Message,
                User = ctx.User
            };

            _eventQueue.Enqueue(new ErrorTestNotification(eventCtx, "Error 1", 0));

            _eventQueue.Enqueue(new ErrorTestNotification(eventCtx, "Error 2", 2000));

            //_eventQueue.Enqueue(new ErrorTestNotification(eventCtx, "Error 3", 0));

            return Task.CompletedTask;
        }
    }
}
