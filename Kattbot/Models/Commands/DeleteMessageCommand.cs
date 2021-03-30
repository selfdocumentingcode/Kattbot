using Kattbot.Services;
using System.Threading.Tasks;

namespace Kattbot.Models.Commands
{
    public class DeleteMessageCommand : EmoteCommand
    {
        private readonly MessageIdPayload _todoMessageId;

        public DeleteMessageCommand(MessageIdPayload todoMessageId)
        {
            _todoMessageId = todoMessageId;
        }

        public override async Task ExecuteAsync(EmoteCommandReceiver emoteCommandReceiver)
        {
            await ExecuteWithRetry(async () => await emoteCommandReceiver.DeleteMessage(_todoMessageId));
        }
    }
}
