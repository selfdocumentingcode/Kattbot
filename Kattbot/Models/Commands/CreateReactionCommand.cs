using Kattbot.Services;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public class CreateReactionCommand : EmoteCommand
    {
        private readonly ReactionCommandPayload _todoReaction;

        public CreateReactionCommand(ReactionCommandPayload todoReaction)
        {
            _todoReaction = todoReaction;
        }

        public override async Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver)
        {
            await ExecuteWithRetry(async () => await emoteCommandReceiver.CreateReaction(_todoReaction));
        }
    }
}
