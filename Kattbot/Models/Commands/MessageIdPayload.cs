using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.Models
{
    public class MessageIdPayload : CommandPayload
    {
        public ulong MessageId { get; set; }

        public MessageIdPayload(ulong messageId, DiscordGuild guild)
        {
            MessageId = messageId;
            Source = EmoteSource.Message;
            Guild = guild;
        }
    }
}
