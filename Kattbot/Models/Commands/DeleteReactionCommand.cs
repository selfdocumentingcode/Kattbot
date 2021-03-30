using Kattbot.Services;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public class DeleteReactionCommand : EmoteCommand
    {
        private readonly ReactionCommandPayload _todoReaction;

        public DeleteReactionCommand(ReactionCommandPayload todoReaction)
        {
            _todoReaction = todoReaction;
        }

        public override async Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver)
        {
            await ExecuteWithRetry(async () => await emoteCommandReceiver.DeleteReaction(_todoReaction));
        }
    }
}
