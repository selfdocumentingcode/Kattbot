using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
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

            sb.AppendLine($"`{commandPrefix}stats user [username] [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats me [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats best [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats worst [?interval] [?page]`");
            sb.AppendLine($"`{commandPrefix}stats emote [emote] [?interval]`");
            sb.AppendLine($"`{commandPrefix}prep me`");
            sb.AppendLine($"`{commandPrefix}prep user [username]`");
            sb.AppendLine($"`{commandPrefix}meow`");
            sb.AppendLine($"(\"?\" denotes an optional parameter)");
            sb.AppendLine();
            sb.AppendLine($"Help commands");
            sb.AppendLine($"`{commandPrefix}stats help`");
            sb.AppendLine($"`{commandPrefix}sc help (speaking club)`");

            var result = FormattedResultHelper.BuildMessage($"Commands", sb.ToString());

            return ctx.RespondAsync(result);
        }        
    }
}
