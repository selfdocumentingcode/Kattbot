using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Common.Utils;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.KattGpt;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
[RequireOwner]
[Group("admin")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class AdminModule : BaseCommandModule
{
    private readonly BotUserRolesRepository _botUserRolesRepo;
    private readonly KattGptChannelCache _cache;
    private readonly GuildSettingsService _guildSettingsService;
    private readonly KattGptService _kattGptService;

    public AdminModule(
        BotUserRolesRepository botUserRolesRepo,
        GuildSettingsService guildSettingsService,
        KattGptChannelCache cache,
        KattGptService kattGptService)
    {
        _botUserRolesRepo = botUserRolesRepo;
        _guildSettingsService = guildSettingsService;
        _cache = cache;
        _kattGptService = kattGptService;
    }

    [Command("nickname")]
    public async Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
    {
        name = name.RemoveQuotes();

        await ctx.Guild.CurrentMember.ModifyAsync(props => { props.Nickname = name; });
    }

    [Command("add-friend")]
    public async Task AddFriend(CommandContext ctx, DiscordMember member)
    {
        ulong userId = member.Id;
        string username = member.DisplayName;
        var friendRole = BotRoleType.Friend;

        bool hasRole = await _botUserRolesRepo.UserHasRole(userId, friendRole);

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
        ulong userId = member.Id;
        string username = member.DisplayName;
        var friendRole = BotRoleType.Friend;

        bool hasRole = await _botUserRolesRepo.UserHasRole(userId, friendRole);

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
        ulong channelId = channel.Id;
        ulong guildId = channel.GuildId!.Value;

        await _guildSettingsService.SetBotChannel(guildId, channelId);

        await ctx.RespondAsync($"Set bot channel to #{channel.Name}");
    }

    [Command("dump-prompts")]
    public async Task DumpPrompts(CommandContext ctx, DiscordChannel channel)
    {
        List<ChatCompletionMessage> systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);

        if (systemPromptsMessages.Count == 0)
        {
            await ctx.RespondAsync("No prompts found");
            return;
        }

        await SendDumpReply(ctx, systemPromptsMessages);
    }

    [Command("dump-context")]
    public async Task DumpContext(CommandContext ctx, DiscordChannel channel)
    {
        string cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);

        KattGptChannelContext? channelContext = _cache.GetCache(cacheKey);

        if (channelContext == null)
        {
            await ctx.RespondAsync("No messages found");
            return;
        }

        List<ChatCompletionMessage> contextMessages = channelContext.GetMessages();

        await SendDumpReply(ctx, contextMessages);
    }

    private static async Task SendDumpReply(CommandContext ctx, List<ChatCompletionMessage> contextMessages)
    {
        var tokenizer = new KattGptTokenizer("gpt-4o");

        int tokenCount = tokenizer.GetTokenCount(contextMessages);

        var sb = new StringBuilder($"Chat messages. Context size: {tokenCount} tokens");
        sb.AppendLine();

        foreach (ChatCompletionMessage message in contextMessages)
        {
            sb.AppendLine(message.ToString());
        }

        string responseMessage = sb.ToString().TrimEnd();

        if (responseMessage.Length <= DiscordConstants.MaxMessageLength)
        {
            await ctx.RespondAsync(responseMessage);
            return;
        }

        List<string> messageChunks = responseMessage.SplitString(DiscordConstants.MaxMessageLength, string.Empty);

        foreach (string messageChunk in messageChunks)
        {
            await ctx.RespondAsync(messageChunk);
        }
    }
}
