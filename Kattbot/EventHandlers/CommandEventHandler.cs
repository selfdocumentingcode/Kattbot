using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Config;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.EventHandlers
{
    public class CommandEventHandler : BaseEventHandler
    {
        private readonly BotOptions _options;
        private readonly ILogger<CommandEventHandler> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly GuildSettingsService _guildSettingsService;

        public CommandEventHandler(
            IOptions<BotOptions> options,
            ILogger<CommandEventHandler> logger,
            DiscordErrorLogger discordErrorLogger,
            GuildSettingsService guildSettingsService)
        {
            _options = options.Value;
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
            _guildSettingsService = guildSettingsService;
        }

        public void RegisterHandlers(CommandsNextExtension commands)
        {
            commands.CommandExecuted += OnCommandExecuted;
            commands.CommandErrored += OnCommandErrored;
        }

        private Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            var message = e.Context.Message;

            var messageContent = message.Content;
            var username = message.Author.Username;

            _logger.LogDebug($"Command: {username} -> {messageContent}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Try to find a suitable error message to return to the user
        /// if command was executed in a bot channel, otherwise add a reaction.
        /// Log error to discord logger.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var channelId = ctx.Channel.Id;
            var message = ctx.Message;
            var exception = e.Exception;

            var commandExecutedInDm = ctx.Channel.IsPrivate;

            var commandPrefix = _options.CommandPrefix;
            var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

            string errorMessage = string.Empty;

            // Flag unknown commands and not return any error message in this case
            // as it's easy for users to accidentally trigger commands using the prefix
            bool isUnknownCommand = false;
            bool appendHelpText = false;

            const string unknownSubcommandErrorString = "No matching subcommands were found, and this group is not executable.";
            const string unknownOverloadErrorString = "Could not find a suitable overload for the command.";

            // DM commands are handled separately
            var isChecksFailedException = !commandExecutedInDm && exception is ChecksFailedException;

            var isUnknownCommandException = exception is CommandNotFoundException;
            var isUnknownSubcommandException = exception.Message == unknownSubcommandErrorString;
            var isUnknownOverloadException = exception.Message == unknownOverloadErrorString;

            var isCommandConfigException = exception is DuplicateCommandException
                                    || exception is DuplicateOverloadException
                                    || exception is InvalidOverloadException;

            // TODO: If this isn't enough, create a custom exception class for validation errors
            var isPossiblyValidationException = exception is ArgumentException;

            if (isUnknownCommandException)
            {
                errorMessage = $"I do not recognize your command.";
                isUnknownCommand = true;
                appendHelpText = true;
            }
            else if (isUnknownSubcommandException)
            {
                errorMessage = $"I do not recognize your command.";
                appendHelpText = true;
            }
            else if (isUnknownOverloadException)
            {
                errorMessage = $"Command arguments are (probably) incorrect.";
                appendHelpText = true;
            }
            else if (isCommandConfigException)
            {
                errorMessage = $"Something's not quite right.";
                appendHelpText = true;
            }
            else if (isChecksFailedException)
            {
                var checksFailedException = (ChecksFailedException)exception;

                var failedCheck = checksFailedException.FailedChecks[0];

                if (failedCheck is RequireOwnerOrFriend)
                {
                    errorMessage = "You do not have permission to do that.";
                }
                else if (failedCheck is CooldownAttribute cooldDownAttribute)
                {
                    errorMessage = $"Please wait {cooldDownAttribute.Reset.TotalSeconds} seconds";
                }
                else
                {
                    errorMessage = "Preexecution check failed.";
                }
            }
            else if (isPossiblyValidationException)
            {
                errorMessage = $"{exception.Message}.";
                appendHelpText = true;
            }

            if (!commandExecutedInDm)
            {
                var botChannelId = await _guildSettingsService.GetBotChannelId(ctx.Guild.Id);

                var isCommandInBotChannel = botChannelId.HasValue && botChannelId.Value == channelId;

                if (isCommandInBotChannel)
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "Something went wrong.";
                    }

                    if (appendHelpText)
                    {
                        errorMessage += $" {commandHelpText}";
                    }

                    await message.RespondAsync(errorMessage);
                }
                else if (!isUnknownCommand)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
                }
            }

            var isUnhandledException =
                   !isUnknownCommandException
                && !isUnknownSubcommandException
                && !isCommandConfigException
                && !isChecksFailedException
                && !isPossiblyValidationException
                && !commandExecutedInDm;

            if (isUnhandledException)
            {
                _discordErrorLogger.LogError(ctx, exception.ToString());
            }

            if (commandExecutedInDm)
            {
                _discordErrorLogger.LogError(ctx, "Command executed in DM");
            }

            _logger.LogWarning("Message: {MessageContent}\r\nCommand failed: {Exception})", message.Content, exception);
        }
    }
}
