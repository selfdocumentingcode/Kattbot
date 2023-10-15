using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Kattbot.Attributes
{
    /// <summary>
    /// Reject commands coming from DM.
    /// </summary>
    public class BaseCommandCheck : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var channel = ctx.Channel;

            bool allowCommand = !channel.IsPrivate;

            return Task.FromResult(allowCommand);
        }
    }
}
