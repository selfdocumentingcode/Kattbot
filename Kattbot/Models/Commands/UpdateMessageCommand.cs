using Kattbot.Services;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public class UpdateMessageCommand : EmoteCommand
    {
        private readonly MessageCommandPayload _todoMessage;

        public UpdateMessageCommand(MessageCommandPayload todoMessage)
        {
            _todoMessage = todoMessage;
        }

        public override async Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver)
        {
            await ExecuteWithRetry(async () => await emoteCommandReceiver.UpdateMessage(_todoMessage));
        }
    }
}
