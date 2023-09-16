using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.KattGpt;
using Microsoft.Extensions.Logging;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    [RequireOwner]
    [Group("admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminModule : BaseCommandModule
    {
        private readonly ILogger<AdminModule> _logger;
        private readonly BotUserRolesRepository _botUserRolesRepo;
        private readonly GuildSettingsService _guildSettingsService;
        private readonly KattGptChannelCache _cache;
        private readonly KattGptService _kattGptService;

        public AdminModule(
            ILogger<AdminModule> logger,
            BotUserRolesRepository botUserRolesRepo,
            GuildSettingsService guildSettingsService,
            KattGptChannelCache cache,
            KattGptService kattGptService)
        {
            _logger = logger;
            _botUserRolesRepo = botUserRolesRepo;
            _guildSettingsService = guildSettingsService;
            _cache = cache;
            _kattGptService = kattGptService;
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
        public async Task AddFriend(CommandContext ctx, DiscordMember member)
        {
            var userId = member.Id;
            var username = member.DisplayName;
            var friendRole = BotRoleType.Friend;

            var hasRole = await _botUserRolesRepo.UserHasRole(userId, friendRole);

            if (hasRole)
            {
                await ctx.RespondAsync("User already has role");
                return;
            }

            await _botUserRolesRepo.AddUserRole(userId, friendRole);

            await ctx.RespondAsync($"{username} is now a friend of Kattbot's");
        }

        [Command("remove-friend")]
        public async Task RemoveFriend(CommandContext ctx, DiscordMember member)
        {
            var userId = member.Id;
            var username = member.DisplayName;
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

        [Command("set-bot-channel")]
        public async Task SetBotChannel(CommandContext ctx, DiscordChannel channel)
        {
            var channelId = channel.Id;
            var guildId = channel.GuildId!.Value;

            await _guildSettingsService.SetBotChannel(guildId, channelId);

            await ctx.RespondAsync($"Set bot channel to #{channel.Name}");
        }

        [Command("dump-prompts")]
        public async Task DumpPrompts(CommandContext ctx, DiscordChannel channel)
        {
            var systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);
            var tokenCount = _kattGptService.GetTokenCount(systemPromptsMessages);

            var sb = new StringBuilder($"System prompt messages. Context size {tokenCount} tokens");
            sb.AppendLine();

            foreach (var message in systemPromptsMessages)
            {
                sb.AppendLine();
                sb.AppendLine($"> {message.Content}");
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("dump-context")]
        public async Task DumpContext(CommandContext ctx, DiscordChannel channel)
        {
            var cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);

            var boundedMessageQueue = _cache.GetCache(cacheKey);

            if (boundedMessageQueue == null)
            {
                await ctx.RespondAsync("No prompts found");
                return;
            }

            var contextMessages = boundedMessageQueue.GetAll();

            var tokenCount = _kattGptService.GetTokenCount(contextMessages);

            var sb = new StringBuilder($"Chat messages. Context size: {tokenCount} tokens");
            sb.AppendLine();

            foreach (var message in contextMessages)
            {
                sb.AppendLine($"{message.Role}:");
                sb.AppendLine($"> {message.Content}");
            }

            await ctx.RespondAsync(sb.ToString());
        }
    }
}
