using DSharpPlus.CommandsNext;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
