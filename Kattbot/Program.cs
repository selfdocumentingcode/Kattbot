using System;
using System.Threading.Channels;
using DSharpPlus;
using Kattbot.CommandHandlers;
using Kattbot.Config;
using Kattbot.Data;
using Kattbot.Data.Repositories;
using Kattbot.EventHandlers;
using Kattbot.Helpers;
using Kattbot.Infrastructure;
using Kattbot.Services;
using Kattbot.Services.Cache;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using Kattbot.Workers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.OptionsKey));
                services.Configure<KattGptOptions>(hostContext.Configuration.GetSection(KattGptOptions.OptionsKey));

                services.AddHttpClient();
                services.AddHttpClient<ChatGptHttpClient>();
                services.AddHttpClient<DalleHttpClient>();

                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssemblyContaining<Program>();
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
                });
                services.AddSingleton<NotificationPublisher>();

                services.AddSingleton<SharedCache>();
                services.AddSingleton<KattGptChannelCache>();
                services.AddSingleton<PuppeteerFactory>();

                AddWorkers(services);

                AddChannels(services);

                AddBotEventHandlers(services);

                AddInternalServices(services);

                AddRepositories(services);

                AddDbContext(hostContext, services);

                AddDiscordClient(hostContext, services);
            })
        .UseSystemd();
    }

    private static void AddDiscordClient(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddSingleton((_) =>
        {
            var defaultLogLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Warning";
            var botToken = hostContext.Configuration.GetValue<string>("Kattbot:BotToken") ?? throw new Exception("Bot token not found");

            LogLevel logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

            var socketConfig = new DiscordConfiguration
            {
                MinimumLogLevel = logLevel,
                TokenType = TokenType.Bot,
                Token = botToken,
                Intents = DiscordIntents.All,
            };

            var client = new DiscordClient(socketConfig);

            return client;
        });
    }

    private static void AddDbContext(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddDbContext<KattbotContext>(
                            builder =>
                            {
                                var dbConnString = hostContext.Configuration.GetValue<string>("Kattbot:ConnectionString");
                                var logLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");

                                builder.EnableSensitiveDataLogging(logLevel == "Debug");

                                builder.UseNpgsql(dbConnString);
                            },
                            ServiceLifetime.Transient,
                            ServiceLifetime.Singleton);
    }

    private static void AddBotEventHandlers(IServiceCollection services)
    {
        services.AddSingleton<CommandEventHandler>();
        services.AddSingleton<EmoteEventHandler>();
    }

    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddTransient<EmoteEntityBuilder>();
        services.AddTransient<DateTimeProvider>();
        services.AddTransient<EmoteParser>();
        services.AddTransient<GuildSettingsService>();
        services.AddTransient<ImageService>();
        services.AddTransient<DiscordErrorLogger>();
        services.AddTransient<PetPetClient>();
        services.AddTransient<DiscordResolver>();
        services.AddTransient<KattGptService>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddTransient<EmotesRepository>();
        services.AddTransient<EmoteStatsRepository>();
        services.AddTransient<BotUserRolesRepository>();
        services.AddTransient<GuildSettingsRepository>();
    }

    private static void AddWorkers(IServiceCollection services)
    {
        services.AddHostedService<CommandQueueWorker>();
        services.AddHostedService<EventQueueWorker>();
        services.AddHostedService<DiscordLoggerWorker>();
        services.AddHostedService<BotWorker>();
    }

    private static void AddChannels(IServiceCollection services)
    {
        const int channelSize = 1024;

        services.AddSingleton((_) => new CommandQueueChannel(Channel.CreateBounded<CommandRequest>(channelSize)));
        services.AddSingleton((_) => new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
        services.AddSingleton((_) => new DiscordLogChannel(Channel.CreateBounded<DiscordLogItem>(channelSize)));
    }
}
