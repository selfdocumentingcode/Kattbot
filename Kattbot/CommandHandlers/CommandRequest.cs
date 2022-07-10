using DSharpPlus.CommandsNext;
using MediatR;

namespace Kattbot.CommandHandlers
{
    public class CommandRequest: IRequest
    {
        public CommandContext Ctx { get; private set; }

        public CommandRequest(CommandContext ctx)
        {
            Ctx = ctx;
        }
    }
}
