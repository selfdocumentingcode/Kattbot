using Kattbot.Services;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public class CreateMessageCommand : EmoteCommand
    {
        private readonly MessageCommandPayload _todoMessage;

        public CreateMessageCommand(MessageCommandPayload todoMessage)
        {
            _todoMessage = todoMessage;
        }

        public override async Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver)
        {
            await ExecuteWithRetry(async () => await emoteCommandReceiver.CreateMessage(_todoMessage));
        }
    }
}
