using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Clients;
using DSharpPlus.Extensions;
using Kattbot.Data;
using Kattbot.NotificationHandlers;
using Kattbot.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    private static void RegisterEventHandlers(this DiscordClientBuilder builder)
    {
        builder.ConfigureEventHandlers(cfg =>
        {
            cfg.HandleMessageCreated((client, args) =>
                client.WriteNotification(new MessageCreatedNotification(args)));
            cfg.HandleMessageUpdated((client, args) =>
                client.WriteNotification(new MessageUpdatedNotification(args)));
            cfg.HandleMessageDeleted((client, args) =>
                client.WriteNotification(new MessageDeletedNotification(args)));
            cfg.HandleMessagesBulkDeleted((client, args) =>
                client.WriteNotification(new MessageBulkDeletedNotification(args)));
            cfg.HandleMessageReactionAdded((client, args) =>
                client.WriteNotification(new MessageReactionAddedNotification(args)));
            cfg.HandleMessageReactionRemoved((client, args) =>
                client.WriteNotification(new MessageReactionRemovedNotification(args)));
        });
    }

    private static async Task WriteNotification<T>(this DiscordClient client, T notification)
        where T : EventNotification
    {
        var eventQueue = client.ServiceProvider.GetRequiredService<EventQueueChannel>();
        await eventQueue.Writer.WriteAsync(notification);
    }
}
