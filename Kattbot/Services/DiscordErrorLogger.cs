using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Services
{
    public class DiscordErrorLogger
    {
        private readonly DiscordClient _client;
        private readonly BotOptions _options;

        public DiscordErrorLogger(
            DiscordClient client,
            IOptions<BotOptions> options
            )
        {
            _client = client;
            _options = options.Value;
        }

        public async Task LogDiscordError(CommandContext ctx, string errorMessage)
        {
            var user = $"{ctx.User.Username}#{ctx.User.Discriminator}";
            var channelName = ctx.Channel.Name;
            var guildName = ctx.Guild.Name;
            var command = EscapeTicks(ctx.Message.Content);

            var contextMessage = $"**Failed command** `{command}` by `{user}` in `{channelName}`(`{guildName}`)";
            var escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

            var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

            await LogDiscordError(fullErrorMessage);
        }

        public async Task LogDiscordError(EventErrorContext ctx, string errorMessage)
        {
            var user = ctx.User != null ? $"{ctx.User.Username}#{ctx.User.Discriminator}" : string.Empty;
            var channelName = ctx.Channel?.Name ?? string.Empty;
            var guildName = ctx.Guild?.Name ?? string.Empty;
            var eventName = ctx.EventName;
            var message = ctx.Message != null ? EscapeTicks(ctx.Message.Content) : string.Empty;

            var contextMessage = $"**Failed event** `{eventName}` by `{user}` in `{channelName}`(`{guildName}`)";
            if (!string.IsNullOrWhiteSpace(message)) 
                contextMessage += $"{Environment.NewLine}Message: `{message}`";
            var escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

            var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

            await LogDiscordError(fullErrorMessage);
        }

        private async Task LogDiscordError(string error)
        {
            var errorLogGuilId = _options.ErrorLogGuildId;
            var errorLogChannelId = _options.ErrorLogChannelId;

            var errorLogChannel = await ResolveErrorLogChannel(errorLogGuilId, errorLogChannelId);

            if (errorLogChannel != null)
            {
                await errorLogChannel.SendMessageAsync(error);
            }
        }

        private static string EscapeTicks(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Replace('`', '\'');
        }

        private async Task<DiscordChannel> ResolveErrorLogChannel(ulong guildId, ulong channelId)
        {
            _client.Guilds.TryGetValue(guildId, out var discordGuild);

            if (discordGuild == null)
            {
                discordGuild = await _client.GetGuildAsync(guildId);
            }

            discordGuild.Channels.TryGetValue(channelId, out var discordChannel);

            if (discordChannel == null)
            {
                discordChannel = discordGuild.GetChannel(channelId);
            }

            return discordChannel;
        }
    }
}
