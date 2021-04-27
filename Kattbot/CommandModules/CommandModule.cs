using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    public class CommandModule : BaseCommandModule
    {
        private readonly BotOptions _options;

        public CommandModule(
            IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        [Command("help")]
        [Description("Return some help")]
        public Task GetHelp(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine($"`{commandPrefix}stats me [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats best [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats emote [emote] [?interval]`");
            sb.AppendLine($"`{commandPrefix}stats help`");

            sb.AppendLine();
            sb.AppendLine("Other commands");
            sb.AppendLine($"`{commandPrefix}prep me`");
            sb.AppendLine($"`{commandPrefix}prep user [username]`");
            sb.AppendLine($"`{commandPrefix}meow`");
            sb.AppendLine($"`{commandPrefix}big [emote]`");
            sb.AppendLine($"`{commandPrefix}sc help (speaking club)`");

            sb.AppendLine($"*(\"?\" denotes an optional parameter)*");

            var result = FormattedResultHelper.BuildMessage($"Commands", sb.ToString());

            return ctx.RespondAsync(result);
        }        
    }
}
