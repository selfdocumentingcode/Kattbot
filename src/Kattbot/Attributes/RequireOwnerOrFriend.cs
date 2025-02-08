using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Data.Repositories;

namespace Kattbot.Attributes;

// Basically a Kattbot moderator (or owner)
public class RequireOwnerOrFriend : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        object? botUserRolesRepoObj = ctx.Services.GetService(typeof(BotUserRolesRepository));

        if (botUserRolesRepoObj == null)
        {
            throw new Exception("Could not fetch BotUserRolesRepository");
        }

        var botUserRolesRepo = (BotUserRolesRepository)botUserRolesRepoObj;

        ulong userId = ctx.User.Id;

        DiscordApplication botApp = ctx.Client.CurrentApplication;

        bool isBotOwner = botApp.Owners.Any(x => x.Id == userId);

        if (isBotOwner)
        {
            return true;
        }

        bool hasFriendRole = await botUserRolesRepo.UserHasRole(userId, BotRoleType.Friend);

        return hasFriendRole;
    }
}
