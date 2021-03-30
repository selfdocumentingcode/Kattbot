using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Data;
using Kattbot.Data.Repositories;
using Kattbot.Helper;
using Kattbot.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [Group("utils")]
    public class UtilsModule : BaseCommandModule
    {
        private readonly ILogger<UtilsModule> _logger;
        private readonly DiscordClient _client;

        public UtilsModule(
            ILogger<UtilsModule> logger,
            DiscordClient client)
        {
            _logger = logger;
            _client = client;
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
            var guildId = ctx.Guild.Id;

            var guild = _client.Guilds[guildId];

            var role = guild.Roles.
                FirstOrDefault(kv => kv.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));

            if (role.Equals(default(KeyValuePair<ulong, DiscordRole>)))
            {
                await ctx.RespondAsync($"Role {roleName} doesn't exist");
            }
            else
            {
                await ctx.RespondAsync($"Role {roleName} has id: {role.Value.Id}");
            }
        }
    }
}
