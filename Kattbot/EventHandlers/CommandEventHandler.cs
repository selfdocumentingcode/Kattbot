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

namespace Kattbot.EventHandlers;

public class CommandEventHandler : BaseEventHandler
{
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly GuildSettingsService _guildSettingsService;
    private readonly ILogger<CommandEventHandler> _logger;
    private readonly BotOptions _options;

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
        DiscordMessage message = e.Context.Message;

        string messageContent = message.Content;
        string username = message.Author.Username;

        _logger.LogDebug($"Command: {username} -> {messageContent}");

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Try to find a suitable error message to return to the user
    ///     if command was executed in a bot channel, otherwise add a reaction.
    ///     Log error to discord logger.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
    {
        CommandContext ctx = e.Context;
        ulong channelId = ctx.Channel.Id;
        DiscordMessage message = ctx.Message;
        Exception exception = e.Exception;

        bool commandExecutedInDm = ctx.Channel.IsPrivate;

        string commandPrefix = _options.CommandPrefix;
        var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

        var errorMessage = string.Empty;

        // Flag unknown commands and not return any error message in this case
        // as it's easy for users to accidentally trigger commands using the prefix
        var isUnknownCommand = false;
        var appendHelpText = false;

        const string unknownSubcommandErrorString =
            "No matching subcommands were found, and this group is not executable.";
        const string unknownOverloadErrorString = "Could not find a suitable overload for the command.";

        // DM commands are handled separately
        bool isChecksFailedException = !commandExecutedInDm && exception is ChecksFailedException;

        bool isUnknownCommandException = exception is CommandNotFoundException;
        bool isUnknownSubcommandException = exception.Message == unknownSubcommandErrorString;
        bool isUnknownOverloadException = exception.Message == unknownOverloadErrorString;

        bool isCommandConfigException = exception is DuplicateCommandException
                                        || exception is DuplicateOverloadException
                                        || exception is InvalidOverloadException;

        // TODO: If this isn't enough, create a custom exception class for validation errors
        bool isPossiblyValidationException = exception is ArgumentException;

        if (isUnknownCommandException)
        {
            errorMessage = "I do not recognize your command.";
            isUnknownCommand = true;
            appendHelpText = true;
        }
        else if (isUnknownSubcommandException)
        {
            errorMessage = "I do not recognize your command.";
            appendHelpText = true;
        }
        else if (isUnknownOverloadException)
        {
            errorMessage = "Command arguments are (probably) incorrect.";
            appendHelpText = true;
        }
        else if (isCommandConfigException)
        {
            errorMessage = "Something's not quite right.";
            appendHelpText = true;
        }
        else if (isChecksFailedException)
        {
            var checksFailedException = (ChecksFailedException)exception;

            CheckBaseAttribute failedCheck = checksFailedException.FailedChecks[0];

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
            ulong? botChannelId = await _guildSettingsService.GetBotChannelId(ctx.Guild.Id);

            bool isCommandInBotChannel = botChannelId.HasValue && botChannelId.Value == channelId;

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

        bool isUnhandledException =
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
