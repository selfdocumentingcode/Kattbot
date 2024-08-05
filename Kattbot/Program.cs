using System.Threading.Channels;
using Kattbot.CommandHandlers;
using Kattbot.Config;
using Kattbot.Data.Repositories;
using Kattbot.EventHandlers;
using Kattbot.Helpers;
using Kattbot.Infrastructure;
using Kattbot.Services;
using Kattbot.Services.Cache;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using Kattbot.Services.Speech;
using Kattbot.Workers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            .ConfigureServices(
                (hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    services.Configure<BotOptions>(hostContext.Configuration.GetSection(BotOptions.OptionsKey));
                    services.Configure<KattGptOptions>(hostContext.Configuration.GetSection(KattGptOptions.OptionsKey));

                    services.AddHttpClient();
                    services.AddHttpClient<ChatGptHttpClient>();
                    services.AddHttpClient<DalleHttpClient>();
                    services.AddHttpClient<SpeechHttpClient>();

                    services.AddMediatR(
                        cfg =>
                        {
                            cfg.RegisterServicesFromAssemblyContaining<Program>();
                            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
                        });
                    services.AddSingleton<NotificationPublisher>();

                    services.AddSingleton<SharedCache>();
                    services.AddSingleton<KattGptChannelCache>();

                    AddWorkers(services);

                    AddChannels(services);

                    AddBotEventHandlers(services);

                    AddInternalServices(services);

                    AddRepositories(services);

                    services.AddDbContext(configuration);

                    services.AddDiscordClient(configuration);
                })
            .UseSystemd();
    }

    private static void AddBotEventHandlers(IServiceCollection services)
    {
        services.AddSingleton<CommandEventHandler>();
    }

    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddTransient<EmoteEntityBuilder>();
        services.AddTransient<DateTimeProvider>();
        services.AddTransient<EmoteParser>();
        services.AddTransient<GuildSettingsService>();
        services.AddTransient<ImageService>();
        services.AddTransient<DiscordErrorLogger>();
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

        services.AddSingleton(new CommandQueueChannel(Channel.CreateBounded<CommandRequest>(channelSize)));
        services.AddSingleton(new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
        services.AddSingleton(new DiscordLogChannel(Channel.CreateBounded<BaseDiscordLogItem>(channelSize)));
    }
}
