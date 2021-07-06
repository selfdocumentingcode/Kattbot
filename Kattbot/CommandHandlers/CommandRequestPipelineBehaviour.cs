using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers
{
    public class CommandRequestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly GuildSettingsService _guildSettingsService;

        public CommandRequestPipelineBehaviour(
            ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> logger,
            DiscordErrorLogger discordErrorLogger,
            GuildSettingsService guildSettingsService
            )
        {
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
            _guildSettingsService = guildSettingsService;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
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
                    return default(TResponse)!;
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task HandeCommandRequestException(CommandRequest request, Exception ex)
        {
            var ctx = request.Ctx;

            var guildId = ctx.Guild.Id;
            var channelId = ctx.Channel.Id;

            var botChannelId = await _guildSettingsService.GetBotChannelId(guildId);

            var isCommandInBotChannel = botChannelId != null && botChannelId.Value == channelId;

            if (isCommandInBotChannel)
            {
                await ctx.RespondAsync(ex.Message);
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
            }

            var escapedError = DiscordErrorLogger.ReplaceTicks(ex.ToString());
            await _discordErrorLogger.LogDiscordError($"`{escapedError}`");

            _logger.LogError(ex, nameof(HandeCommandRequestException));
        }
    }
}
