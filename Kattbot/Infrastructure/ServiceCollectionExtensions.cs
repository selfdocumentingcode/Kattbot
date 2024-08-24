using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Extensions;
using DSharpPlus.Net.Gateway;
using Kattbot.Attributes;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Config;
using Kattbot.Data;
using Kattbot.Helpers;
using Kattbot.NotificationHandlers;
using Kattbot.Services;
using Kattbot.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddDiscordClient(this IServiceCollection services, IConfiguration configuration)
    {
        string defaultLogLevel = configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Warning";
        string botToken = configuration.GetValue<string>("Kattbot:BotToken")
                          ?? throw new Exception("Bot token not found");

        var logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

        var clientBuilder = DiscordClientBuilder.CreateDefault(botToken, DiscordIntents.All, services);

        clientBuilder.SetLogLevel(logLevel);

        clientBuilder.RegisterCommands(configuration);

        clientBuilder.RegisterEventHandlers();

        // This replacement has to happen after the DiscordClientBuilder.CreateDefault call
        // and before the DiscordClient is built.
        services.Replace<IGatewayController, NoWayGateway>();

        // Calling build registers the DiscordClient as a singleton in the service collection
        clientBuilder.Build();
    }

    public static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<KattbotContext>(
            builder =>
            {
                var dbConnString = configuration.GetValue<string>("Kattbot:ConnectionString");
                var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default");

                builder.EnableSensitiveDataLogging(logLevel == "Debug");

                builder.UseNpgsql(dbConnString);
            },
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton);
    }

    private static void RegisterCommands(this DiscordClientBuilder builder, IConfiguration configuration)
    {
        string commandPrefix = configuration.GetValue<string>("Kattbot:CommandPrefix")
                               ?? throw new Exception("Command prefix not found");
        string alternateCommandPrefix = configuration.GetValue<string>("Kattbot:AlternateCommandPrefix")
                                        ?? throw new Exception("Alternate command prefix not found");
        string[] commandPrefixes = [commandPrefix, alternateCommandPrefix];

        var config = new CommandsNextConfiguration
        {
            StringPrefixes = commandPrefixes,
            EnableDefaultHelp = false,
            EnableMentionPrefix = false,
        };

        builder.UseCommandsNext(
            commands =>
            {
                commands.RegisterConverter(new GenericArgumentConverter<StatsCommandArgs, StatsCommandArgsParser>());
                commands.RegisterCommands(Assembly.GetExecutingAssembly());

                commands.CommandExecuted += OnCommandExecuted;
                commands.CommandErrored += OnCommandErrored;
            },
            config);
    }

    private static void RegisterEventHandlers(this DiscordClientBuilder builder)
    {
        builder.ConfigureEventHandlers(
            cfg =>
            {
                cfg.HandleMessageCreated(
                    (client, args) =>
                        client.WriteNotification(new MessageCreatedNotification(args)));
                cfg.HandleMessageUpdated(
                    (client, args) =>
                        client.WriteNotification(new MessageUpdatedNotification(args)));
                cfg.HandleMessageDeleted(
                    (client, args) =>
                        client.WriteNotification(new MessageDeletedNotification(args)));
                cfg.HandleMessagesBulkDeleted(
                    (client, args) =>
                        client.WriteNotification(new MessageBulkDeletedNotification(args)));
                cfg.HandleMessageReactionAdded(
                    (client, args) =>
                        client.WriteNotification(new MessageReactionAddedNotification(args)));
                cfg.HandleMessageReactionRemoved(
                    (client, args) =>
                        client.WriteNotification(new MessageReactionRemovedNotification(args)));
            });
    }

    private static async Task WriteNotification<T>(this DiscordClient client, T notification)
        where T : EventNotification
    {
        var eventQueue = client.ServiceProvider.GetRequiredService<EventQueueChannel>();
        await eventQueue.Writer.WriteAsync(notification);
    }

    private static Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
    {
        IServiceProvider services = sender.Client.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<CommandsNextExtension>>();
        DiscordMessage message = e.Context.Message;

        string messageContent = message.Content;
        string username = message.Author!.Username;

        logger.LogDebug("Command: {username} -> {messageContent}", username, messageContent);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Try to find a suitable error message to return to the user
    ///     if command was executed in a bot channel, otherwise add a reaction.
    ///     Log error to discord logger.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private static async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
    {
        IServiceProvider services = sender.Client.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<CommandsNextExtension>>();
        var discordErrorLogger = services.GetRequiredService<DiscordErrorLogger>();
        BotOptions options = services.GetRequiredService<IOptions<BotOptions>>().Value;
        var guildSettingsService = services.GetRequiredService<GuildSettingsService>();

        CommandContext ctx = e.Context;
        ulong channelId = ctx.Channel.Id;
        DiscordMessage message = ctx.Message;
        Exception exception = e.Exception;

        bool commandExecutedInDm = ctx.Channel.IsPrivate;

        string commandPrefix = options.CommandPrefix;
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
            ulong? botChannelId = await guildSettingsService.GetBotChannelId(ctx.Guild.Id);

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
            discordErrorLogger.LogError(ctx, exception.ToString());
        }

        if (commandExecutedInDm)
        {
            discordErrorLogger.LogError(ctx, "Command executed in DM");
        }

        logger.LogWarning("Message: {MessageContent}\r\nCommand failed: {Exception})", message.Content, exception);
    }
}
