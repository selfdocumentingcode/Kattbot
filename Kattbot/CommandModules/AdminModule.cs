using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Common.Models;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Data;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [RequireOwner]
    [Group("admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminModule : BaseCommandModule
    {
        private readonly ILogger<AdminModule> _logger;
        private readonly BotUserRolesRepository _botUserRolesRepo;
        private readonly GuildSettingsService _guildSettingsService;

        public AdminModule(
            ILogger<AdminModule> logger,
            BotUserRolesRepository botUserRolesRepo,
            GuildSettingsService guildSettingsService)
        {
            _logger = logger;
            _botUserRolesRepo = botUserRolesRepo;
            _guildSettingsService = guildSettingsService;
        }


        [Command("nickname")]
        public async Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
        {
            name = name.RemoveQuotes();

            await ctx.Guild.CurrentMember.ModifyAsync((props) =>
            {
                props.Nickname = name;
            });
        }

        [Command("add-friend")]
        public async Task AddFriend(CommandContext ctx, DiscordMember user)
        {
            var userId = user.Id;
            var username = user.GetNicknameOrUsername();
            var friendRole = BotRoleType.Friend;

            var hasRole = await _botUserRolesRepo.UserHasRole(userId, friendRole);

            if(hasRole)
            {
                await ctx.RespondAsync("User already has role");
                return;
            }

            await _botUserRolesRepo.AddUserRole(userId, friendRole);

            await ctx.RespondAsync($"{username} is now a friend of Kattbot's");
        }

        [Command("remove-friend")]
        public async Task RemoveFriend(CommandContext ctx, DiscordMember user)
        {
            var userId = user.Id;
            var username = user.GetNicknameOrUsername();
            var friendRole = BotRoleType.Friend;

            var hasRole = await _botUserRolesRepo.UserHasRole(userId, friendRole);

            if (!hasRole)
            {
                await ctx.RespondAsync("User does not have role");
                return;
            }

            await _botUserRolesRepo.RemoveUserRole(userId, friendRole);

            await ctx.RespondAsync($"{username} is no longer a friend of Kattbot's");
        }

        [Command("error-test")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ErrorTest(CommandContext ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new Exception("Test error");
        }

        [Command("set-bot-channel")]
        public async Task SetBotChannel(CommandContext ctx, DiscordChannel channel)
        {
            var channelId = channel.Id;
            var guildId = channel.GuildId;

            await _guildSettingsService.SetBotChannel(guildId, channelId);

            await ctx.RespondAsync($"Set bot channel to #{channel.Name}");
        }
    }
}
