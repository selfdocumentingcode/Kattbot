using System.Threading.Channels;
using Kattbot.CommandHandlers;
using Kattbot.Config;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Infrastructure;
using Kattbot.Services;
using Kattbot.Services.Cache;
using Kattbot.Services.GptImages;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using Kattbot.Services.Speech;
using Kattbot.Workers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
ConfigurationManager configuration = builder.Configuration;
IServiceCollection services = builder.Services;

services.Configure<BotOptions>(configuration.GetSection(BotOptions.OptionsKey));
services.Configure<KattGptOptions>(configuration.GetSection(KattGptOptions.OptionsKey));

services.AddHttpClient();
services.AddHttpClient<ChatGptHttpClient>();
services.AddHttpClient<SpeechHttpClient>();
services.AddHttpClient<GptImagesHttpClient>();

services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CommandRequestPipelineBehaviour<,>));
});

// Registered as Transient to match lifetime of MediatR
services.AddTransient<NotificationPublisher>();

AddWorkers(services);

AddChannels(services);

AddInternalServices(services);

AddRepositories(services);

services.AddDbContext(configuration);

services.AddDiscordClient(configuration);

IHost app = builder.Build();

app.Run();

return;

static void AddInternalServices(IServiceCollection services)
{
    services.AddSingleton<SharedCache>();
    services.AddSingleton<KattGptChannelCache>();

    services.AddSingleton<DateTimeProvider>();
    services.AddSingleton<DiscordErrorLogger>();
    services.AddSingleton<DiscordResolver>();

    services.AddScoped<GuildSettingsService>();
    services.AddScoped<ImageService>();
    services.AddScoped<KattGptService>();
}

static void AddRepositories(IServiceCollection services)
{
    services.AddScoped<EmotesRepository>();
    services.AddScoped<EmoteStatsRepository>();
    services.AddScoped<BotUserRolesRepository>();
    services.AddScoped<GuildSettingsRepository>();
}

static void AddWorkers(IServiceCollection services)
{
    services.AddHostedService<CommandQueueWorker>();
    services.AddHostedService<EventQueueWorker>();
    services.AddHostedService<DiscordLoggerWorker>();
    services.AddHostedService<BotWorker>();
}

static void AddChannels(IServiceCollection services)
{
    const int channelSize = 1024;

    services.AddSingleton(new CommandQueueChannel(Channel.CreateBounded<CommandRequest>(channelSize)));
    services.AddSingleton(new EventQueueChannel(Channel.CreateBounded<INotification>(channelSize)));
    services.AddSingleton(new DiscordLogChannel(Channel.CreateBounded<BaseDiscordLogItem>(channelSize)));
}
