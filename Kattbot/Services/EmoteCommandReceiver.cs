using Kattbot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Services
{
    public class EmoteCommandReceiver
    {
        private readonly EmoteMessageService _emoteMessageService;

        public EmoteCommandReceiver(EmoteMessageService emoteMessageService)
        {
            _emoteMessageService = emoteMessageService;
        }

        public async Task<bool> CreateMessage(MessageCommandPayload todoMessage)
        {
            return await _emoteMessageService.CreateEmoteMessage(todoMessage);
        }

        public async Task<bool> UpdateMessage(MessageCommandPayload todoMessage)
        {
            return await _emoteMessageService.UpdateEmoteMessage(todoMessage);
        }

        public async Task<bool> DeleteMessage(MessageIdPayload todoMessage)
        {
            return await _emoteMessageService.RemoveEmoteMessage(todoMessage);
        }

        public async Task<bool> CreateReaction(ReactionCommandPayload todoReaction)
        {
            return await _emoteMessageService.CreateEmoteReaction(todoReaction);
        }

        public async Task<bool> DeleteReaction(ReactionCommandPayload todoReaction)
        {
            return await _emoteMessageService.RemoveEmoteReaction(todoReaction);
        }
    }
}
