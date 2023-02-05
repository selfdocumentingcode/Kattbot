using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.NotificationHandlers;

public class EventContext
{
    public string EventName { get; set; } = string.Empty;

    public DiscordUser? User { get; set; }

    public DiscordChannel Channel { get; set; } = null!;

    public DiscordGuild Guild { get; set; } = null!;

    public DiscordMessage? Message { get; set; }
}

#pragma warning disable SA1402 // File may only contain a single type
public class EmoteEventContext : EventContext
#pragma warning restore SA1402 // File may only contain a single type
{
    public EmoteSource Source { get; set; }
}
