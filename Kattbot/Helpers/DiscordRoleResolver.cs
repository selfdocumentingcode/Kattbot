using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Helpers
{
    public static class DiscordRoleResolver
    {
        public static TryResolveResult TryResolveByName(DiscordGuild guild, string discordRoleName, out DiscordRole discordRole)
        {
            var matchingDiscordRoles = guild.Roles
                             .Where(kv => kv.Value.Name.Contains(discordRoleName, StringComparison.OrdinalIgnoreCase))
                             .ToList();

            if (matchingDiscordRoles.Count == 0)
            {
                discordRole = null!;
                return new TryResolveResult(false, $"No role matches the name {discordRoleName}");
            }
            else if (matchingDiscordRoles.Count > 1)
            {
                discordRole = null!;
                return new TryResolveResult(false, $"More than 1 role matches the name {discordRoleName}");
            }

            discordRole = matchingDiscordRoles[0].Value;

            return new TryResolveResult(true);
        }
    }
}
