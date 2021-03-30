using DSharpPlus;
using DSharpPlus.Entities;
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

        public async Task LogDiscordError(string error)
        {
            var errorLogGuilId = _options.ErrorLogGuildId;
            var errorLogChannelId = _options.ErrorLogChannelId;

            var errorLogChannel = await ResolveErrorLogChannel(errorLogGuilId, errorLogChannelId);

            if (errorLogChannel != null)
            {
                await errorLogChannel.SendMessageAsync(error);
            }
        }

        public static string ReplaceTicks(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return error;

            return error.Replace('`', '\'');
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
