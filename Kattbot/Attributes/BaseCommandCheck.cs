using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot;
using Kattbot.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Attributes
{
    /// <summary>
    /// Reject commands coming from DM
    /// </summary>
    public class BaseCommandCheck : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var message = ctx.Message;
            var channel = ctx.Channel;

            if (IsPrivateMessageChannel(channel))
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        private bool IsPrivateMessageChannel(DiscordChannel channel)
        {
            return channel.IsPrivate;
        }
    }
}
