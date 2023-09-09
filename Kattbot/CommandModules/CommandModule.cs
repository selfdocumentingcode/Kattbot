using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Attributes;
using Kattbot.CommandModules.ResultFormatters;
using Microsoft.Extensions.Options;

namespace Kattbot.CommandModules;

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

        string commandPrefix = _options.CommandPrefix;

        sb.AppendLine($"`{commandPrefix}stats me [?interval] [?page]`");
        sb.AppendLine($"`{commandPrefix}stats best [?interval] [?page]`");
        sb.AppendLine($"`{commandPrefix}stats [emote] [?interval]`");
        sb.AppendLine($"`{commandPrefix}stats help`");

        sb.AppendLine();
        sb.AppendLine("Emote commands");
        sb.AppendLine($"`{commandPrefix}big [emote]`");
        sb.AppendLine($"`{commandPrefix}bigger [emote]`");
        sb.AppendLine($"`{commandPrefix}gigantic [emote]`");
        sb.AppendLine($"`{commandPrefix}humongous [emote]`");

        sb.AppendLine();
        sb.AppendLine("Image commands");
        sb.AppendLine($"`{commandPrefix}deepfry [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}oilpaint [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}pet [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}dallify [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}dalle [text]`");

        sb.AppendLine();
        sb.AppendLine("Other commands");
        sb.AppendLine($"`{commandPrefix}meow`");

        sb.AppendLine($"*(\"?\" denotes an optional parameter)*");

        sb.AppendLine();
        sb.AppendLine("Kattbot source code: github.com/selfdocumentingcode/Kattbot");

        string result = FormattedResultHelper.BuildMessage($"Commands", sb.ToString());

        return ctx.RespondAsync(result);
    }
}
