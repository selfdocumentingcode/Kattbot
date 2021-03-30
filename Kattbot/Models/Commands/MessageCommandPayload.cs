using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.Models
{
    public class MessageCommandPayload : CommandPayload
    {
        public DiscordMessage Message { get; set; } = null!;

        public MessageCommandPayload(DiscordMessage message, DiscordGuild guild)
        {
            Message = message;
            Source = EmoteSource.Message;
            Guild = guild;
        }
    }
}
