using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

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
            GuildSettingsService guildSettingsService
            )
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
        /// Log error to discord logger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var guildId = ctx.Guild.Id;
            var channelId = ctx.Channel.Id;
            var message = ctx.Message;
            var exception = e.Exception;

            var commandPrefix = _options.CommandPrefix;
            var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

            string errorMessage = string.Empty;

            // Flag unknown commands and not return any error message in this case
            // as it's easy for users to accidentally trigger commands using the prefix
            bool isUnknownCommand = false;
            bool appendHelpText = false;

            const string unknownSubcommandErrorString = "No matching subcommands were found, and this group is not executable.";
            const string unknownOverloadErrorString = "Could not find a suitable overload for the command.";

            var isChecksFailedException = exception is ChecksFailedException;

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

                if (failedCheck is BaseCommandCheck)
                {
                    errorMessage = "I do not care for DM commands.";
                }
                else if (failedCheck is RequireOwnerOrFriend)
                {
                    errorMessage = "You do not have permission to do that.";
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

            var botChannelId = await _guildSettingsService.GetBotChannelId(guildId);

            var isCommandInBotChannel = botChannelId != null && botChannelId.Value == channelId;

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
            else
            {
                if (!isUnknownCommand)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
                }

            }

            // Log any unhandled exception
            var shouldLogDiscordError =
                   !isUnknownCommandException
                && !isUnknownSubcommandException
                && !isCommandConfigException
                && !isChecksFailedException
                && !isPossiblyValidationException;

            if (shouldLogDiscordError)
            {
                var escapedError = DiscordErrorLogger.ReplaceTicks(exception.ToString());
                var escapedMessage = DiscordErrorLogger.ReplaceTicks(message.Content);
                await _discordErrorLogger.LogDiscordError($"Message: `{escapedMessage}`\r\nCommand failed: `{escapedError}`)");
            }

            _logger.LogWarning($"Message: {message.Content}\r\nCommand failed: {exception})");
        }
    }
}