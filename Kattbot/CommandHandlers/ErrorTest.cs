using DSharpPlus.CommandsNext;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers;

public class ErrorTestCommand : CommandRequest
{
    public string Message { get; }
    public int Sleep { get; }

    public ErrorTestCommand(CommandContext ctx, string message, int sleep = 0) : base(ctx)
    {
        Message = message;
        Sleep = sleep;
    }
}

public class ErrorTestHandler : AsyncRequestHandler<ErrorTestCommand>
{
    protected override async Task Handle(ErrorTestCommand request, CancellationToken cancellationToken)
    {
        await request.Ctx.RespondAsync($"Start: {request.Message}");

        if (request.Sleep > 0) await Task.Delay(request.Sleep);

        await request.Ctx.RespondAsync($"Done: {request.Message}");

        throw new Exception(request.Message);
    }
}
