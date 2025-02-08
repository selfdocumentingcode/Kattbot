using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Services;

namespace Kattbot.Helpers;

public class DiscordResolver
{
    private readonly DiscordErrorLogger _discordErrorLogger;

    public DiscordResolver(DiscordErrorLogger discordErrorLogger)
    {
        _discordErrorLogger = discordErrorLogger;
    }

    public static TryResolveResult TryResolveRoleByName(
        DiscordGuild guild,
        string discordRoleName,
        out DiscordRole discordRole)
    {
        List<KeyValuePair<ulong, DiscordRole>> matchingDiscordRoles = guild.Roles
            .Where(kv => kv.Value.Name.Contains(discordRoleName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingDiscordRoles.Count == 0)
        {
            discordRole = null!;
            return new TryResolveResult(false, $"No role matches the name {discordRoleName}");
        }

        if (matchingDiscordRoles.Count > 1)
        {
            discordRole = null!;
            return new TryResolveResult(false, $"More than 1 role matches the name {discordRoleName}");
        }

        discordRole = matchingDiscordRoles[0].Value;

        return new TryResolveResult(true);
    }

    public async Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
    {
        bool memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

        if (memberExists) return member;

        try
        {
            return await guild.GetMemberAsync(userId) ??
                   throw new ArgumentException($"Missing member with id {userId}");
        }
        catch (Exception)
        {
            _discordErrorLogger.LogError("Missing member");

            return null;
        }
    }
}
