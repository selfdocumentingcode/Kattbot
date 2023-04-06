using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandHandlers;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.Infrastructure;

public class CommandRequestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> _logger;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly GuildSettingsService _guildSettingsService;

    public CommandRequestPipelineBehaviour(
        ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> logger,
        DiscordErrorLogger discordErrorLogger,
        GuildSettingsService guildSettingsService)
    {
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
        _guildSettingsService = guildSettingsService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (request is CommandRequest commandRequest)
            {
                await HandeCommandRequestException(commandRequest, ex);
                return default!;
            }
            else
            {
                throw;
            }
        }
    }

    private async Task HandeCommandRequestException(CommandRequest request, Exception ex)
    {
        CommandContext ctx = request.Ctx;

        ulong guildId = ctx.Guild.Id;
        ulong channelId = ctx.Channel.Id;

        ulong? botChannelId = await _guildSettingsService.GetBotChannelId(guildId);

        bool isCommandInBotChannel = botChannelId != null && botChannelId.Value == channelId;

        if (isCommandInBotChannel)
        {
            await ctx.RespondAsync(ex.Message);
        }
        else
        {
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
        }

        _discordErrorLogger.LogDiscordError(ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandeCommandRequestException));
    }
}
