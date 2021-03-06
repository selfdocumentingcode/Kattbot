using DSharpPlus;
using Kattbot.Data;
using Kattbot.Data.Repositories;
using Kattbot.EventHandlers;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using MediatR;
using Kattbot.CommandHandlers;
using Kattbot.Workers;

namespace Kattbot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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
                    services.AddHostedService<EmoteCommandQueueWorker>();

                    services.AddSingleton<CommandQueue>();
                    services.AddSingleton<CommandParallelQueue>();
                    services.AddSingleton<EventQueue>();
                    services.AddSingleton<EmoteCommandQueue>();

                    services.AddTransient<EmoteEntityBuilder>();
                    services.AddTransient<EmoteMessageService>();
                    services.AddTransient<EmoteCommandReceiver>();
                    services.AddTransient<DateTimeProvider>();
                    services.AddTransient<EmoteParser>();
                    services.AddTransient<GuildSettingsService>();
                    services.AddTransient<ImageService>();

                    services.AddTransient<DiscordErrorLogger>();

                    services.AddTransient<EmotesRepository>();
                    services.AddTransient<EmoteStatsRepository>();
                    services.AddTransient<BotUserRolesRepository>();
                    services.AddTransient<GuildSettingsRepository>();

                    services.AddDbContext<KattbotContext>(builder =>
                    {
                        var dbConnString = hostContext.Configuration.GetValue<string>("Kattbot:ConnectionString");
                        var logLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");

                        builder.EnableSensitiveDataLogging(logLevel == "Debug");

                        builder.UseNpgsql(dbConnString);
                    },
                    ServiceLifetime.Transient,
                    ServiceLifetime.Singleton);

                    services.AddSingleton((_) =>
                    {
                        var defaultLogLevel = hostContext.Configuration.GetValue<string>("Logging:LogLevel:Default");
                        var botToken = hostContext.Configuration.GetValue<string>("Kattbot:BotToken");

                        var logLevel = Enum.Parse<LogLevel>(defaultLogLevel);

                        var socketConfig = new DiscordConfiguration
                        {
                            MinimumLogLevel = logLevel,
                            TokenType = TokenType.Bot,
                            Token = botToken
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
