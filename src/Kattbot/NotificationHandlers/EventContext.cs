using DSharpPlus.Entities;

namespace Kattbot.NotificationHandlers;

public class EventContext
{
    public string EventName { get; set; } = string.Empty;

    public DiscordUser? User { get; set; }

    public DiscordChannel Channel { get; set; } = null!;

    public DiscordGuild Guild { get; set; } = null!;

    public DiscordMessage? Message { get; set; }
}
