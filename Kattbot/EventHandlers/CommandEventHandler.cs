using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
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

        private async Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            try
            {
                var socketMessage = e.Context.Message;

                if (!IsReleventMessage(socketMessage))
                    return;

                if (IsPrivateMessageChannel(socketMessage.Channel))
                {
                    await socketMessage.Channel.SendMessageAsync("https://cdn.discordapp.com/emojis/740563346599968900.png?v=1");
                    return;
                }

                var message = socketMessage;

                var messageContent = message.Content;
                var username = message.Author.Username;

                _logger.LogDebug($"Command: {username} -> {messageContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReceived");
            }
        }

        private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var guildId = ctx.Guild.Id;
            var channelId = ctx.Channel.Id;
            var message = ctx.Message;
            var errorMessage = e.Exception.Message;

            var commandPrefix = _options.CommandPrefix;
            var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

            const string unknownSubcommandErrorString = "No matching subcommands were found, and this group is not executable.";

            var isUnknownCommandError = e.Exception is CommandNotFoundException;
            var isUnknownSubcommandError = e.Exception.Message == unknownSubcommandErrorString;

            var isCommandConfigError = e.Exception is DuplicateCommandException
                                    || e.Exception is DuplicateOverloadException
                                    || e.Exception is InvalidOverloadException;

            var botChannelId = await _guildSettingsService.GetBotChannelId(guildId);

            var isCommandInBotChannel = botChannelId != null && botChannelId.Value == channelId;

            if (isCommandInBotChannel)
            {
                if (isUnknownCommandError)
                {
                    var commandErrorText = $"I do not recognize your command. {commandHelpText}";
                    await ctx.Channel.SendMessageAsync(commandErrorText);
                }
                else if (isUnknownSubcommandError)
                {
                    var commandErrorText = $"I do not recognize your command. {commandHelpText}";
                    await ctx.Channel.SendMessageAsync(commandErrorText);
                }
                else if (isCommandConfigError)
                {
                    var commandErrorText = $"Something's not quite right. {commandHelpText}";
                    await ctx.Channel.SendMessageAsync(commandErrorText);
                }
                else
                {
                    var commandErrorText = $"Hmm. Your command suffers from a case of **{errorMessage}** {commandHelpText}";
                    await ctx.Channel.SendMessageAsync(commandErrorText);
                }
            }
            else
            {
                // It's easy to trigger a bad command using the prefix
                // So we're not going to react at all if it's not the bot channel
                if (!isUnknownCommandError)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
                }

            }

            var shouldLogDiscordError = !isUnknownCommandError && !isUnknownSubcommandError && !isCommandConfigError;

            if (shouldLogDiscordError)
            {
                var escapedError = DiscordErrorLogger.ReplaceTicks(e.Exception.ToString());
                var escapedMessage = DiscordErrorLogger.ReplaceTicks(message.Content);
                await _discordErrorLogger.LogDiscordError($"Message: `{escapedMessage}`\r\nCommand failed: `{escapedError}`)");
            }

            _logger.LogWarning($"Message: {message.Content}\r\nCommand failed: {e.Exception})");
        }
    }
}