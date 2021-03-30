using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Attributes
{
    // Basically a Kattbot moderator (or owner)
    public class RequireOwnerOrFriend : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var botUserRolesRepoObj = ctx.Services.GetService(typeof(BotUserRolesRepository));

            if (botUserRolesRepoObj == null)
                throw new Exception("Could not fetch BotUserRolesRepository");

            var botUserRolesRepo = (BotUserRolesRepository)botUserRolesRepoObj;

            var userId = ctx.User.Id;

            var botApp = ctx.Client.CurrentApplication;

            var isBotOwner = botApp.Owners.Any(x => x.Id == userId);

            if (isBotOwner)
                return true;         

            var hasFriendRole = await botUserRolesRepo.UserHasRole(userId, BotRoleType.Friend);

            return hasFriendRole;

        }
    }
}
