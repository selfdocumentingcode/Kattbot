using System;
using DSharpPlus;
using Kattbot.CommandHandlers;
using Kattbot.Data;
using Kattbot.Data.Repositories;
using Kattbot.EventHandlers;
using Kattbot.Helpers;
using Kattbot.Services;
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

                services.AddHttpClient();

                services.AddMediatR(typeof(Program));
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
                services.AddSingleton<NotificationPublisher>();

                services.AddSingleton<SharedCache>();

                services.AddHostedService<BotWorker>();
                services.AddHostedService<CommandQueueWorker>();
                services.AddHostedService<CommandParallelQueueWorker>();
                services.AddHostedService<EventQueueWorker>();

                services.AddSingleton<CommandQueue>();
                services.AddSingleton<CommandParallelQueue>();
                services.AddSingleton<EventQueue>();

                services.AddTransient<EmoteEntityBuilder>();
                services.AddTransient<DateTimeProvider>();
                services.AddTransient<EmoteParser>();
                services.AddTransient<GuildSettingsService>();
                services.AddTransient<ImageService>();

                services.AddTransient<DiscordErrorLogger>();

                services.AddTransient<EmotesRepository>();
                services.AddTransient<EmoteStatsRepository>();
                services.AddTransient<BotUserRolesRepository>();
                services.AddTransient<GuildSettingsRepository>();

                services.AddDbContext<KattbotContext>(
                    builder =>
                    {
                        string dbConnString = hostContext.Configuration.GetValue<string>("Kattbot:ConnectionString");
                        string logLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");

                        builder.EnableSensitiveDataLogging(logLevel == "Debug");

                        builder.UseNpgsql(dbConnString);
                    },
                    ServiceLifetime.Transient,
                    ServiceLifetime.Singleton);

                services.AddSingleton((_) =>
                {
                    string defaultLogLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");
                    string botToken = hostContext.Configuration.GetValue<string>("Kattbot:BotToken");

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

                services.AddSingleton<CommandEventHandler>();
                services.AddSingleton<EmoteEventHandler>();
            })
        .UseSystemd();
    }
}
