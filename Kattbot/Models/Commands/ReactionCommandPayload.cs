using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.Models
{
    public class ReactionCommandPayload : CommandPayload
    {
        public DiscordMessage Message { get; set; } = null!;
        public DiscordEmoji Emoji { get; set; } = null!;
        public DiscordUser User { get; set; } = null!;

        public ReactionCommandPayload(DiscordMessage message, DiscordEmoji emoji, DiscordUser user, DiscordGuild guild)
        {
            Message = message;
            Emoji = emoji;
            Source = EmoteSource.Reaction;
            Guild = guild;
            User = user;
        }
    }
}
