using DSharpPlus.CommandsNext;
using MediatR;

namespace Kattbot.CommandHandlers;

public class CommandRequest : IRequest
{
    public CommandRequest(CommandContext ctx)
    {
        Ctx = ctx;
    }

    public CommandContext Ctx { get; private set; }
}
