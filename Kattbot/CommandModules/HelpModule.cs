using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Attributes;
using Kattbot.CommandModules.ResultFormatters;
using Kattbot.Helpers;
using Microsoft.Extensions.Options;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
[Group("help")]
public class HelpModule : BaseCommandModule
{
    private readonly BotOptions _options;

    public HelpModule(
        IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    [GroupCommand]
    [Description("Return some help")]
    public Task GetHelp(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;

        sb.AppendLine($"`{commandPrefix}stats me [?interval] [?page]`");
        sb.AppendLine($"`{commandPrefix}stats best [?interval] [?page]`");
        sb.AppendLine($"`{commandPrefix}stats [emote] [?interval]`");
        sb.AppendLine($"`{commandPrefix}help stats .. See all stats command`");

        sb.AppendLine();
        sb.AppendLine("Emote commands");
        sb.AppendLine($"`{commandPrefix}big [emote]`");
        sb.AppendLine($"`{commandPrefix}bigger [emote]`");
        sb.AppendLine($"`{commandPrefix}gigantic [emote]`");
        sb.AppendLine($"`{commandPrefix}humongous [emote]`");

        sb.AppendLine();
        sb.AppendLine("Image commands");
        sb.AppendLine($"`{commandPrefix}deepfry [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}pet [emote|user|image] [?speed]`");
        sb.AppendLine($"`{commandPrefix}dallify [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}dalle [text]`");
        sb.AppendLine($"`{commandPrefix}help images .. See all image commands`");

        sb.AppendLine();
        sb.AppendLine("Other commands");
        sb.AppendLine($"`{commandPrefix}meow`");

        sb.AppendLine();
        sb.AppendLine($"`\"?\" denotes an optional parameter`");

        sb.AppendLine();
        sb.AppendLine("Kattbot source code: [github.com/selfdocumentingcode/Kattbot](https://github.com/selfdocumentingcode/Kattbot)");

        var eb = EmbedBuilderHelper.BuildSimpleEmbed("Help", sb.ToString());

        return ctx.RespondAsync(eb);
    }

    [Command("stats")]
    [Description("Help about stats")]
    public Task GetStatsHelp(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;

        sb.AppendLine();
        sb.AppendLine($"Command arguments:");
        sb.AppendLine($"`user           .. Discord username with # identifier or @mention`");
        sb.AppendLine($"`emote          .. Discord emote (server emotes only)`");
        sb.AppendLine($"`-p, --page     .. Displays a different page of the result set`");
        sb.AppendLine($"`                  (default 1st page)`");
        sb.AppendLine($"`-i, --interval .. Limits result set to given interval`");
        sb.AppendLine($"`                  (default 2 months)`");
        sb.AppendLine($"`                  Valid interval units: \"m\", \"w\", \"d\"`");
        sb.AppendLine($"`                  Optionally use interval value \"lifetime\"`");
        sb.AppendLine();
        sb.AppendLine($"Usage examples:");
        sb.AppendLine($"`{commandPrefix}stats best`");
        sb.AppendLine($"`{commandPrefix}stats worst --page 2`");
        sb.AppendLine($"`{commandPrefix}stats @someUser --interval 3m`");
        sb.AppendLine($"`{commandPrefix}stats me -p 2 -i 2w`");
        sb.AppendLine($"`{commandPrefix}stats :a_server_emote:`");

        var eb = EmbedBuilderHelper.BuildSimpleEmbed("Check out random emote related stats", sb.ToString());

        return ctx.RespondAsync(eb);
    }

    [Command("images")]
    [Description("Help about images")]
    public Task GetImagesHelp(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;

        sb.AppendLine();
        sb.AppendLine("Commands");
        sb.AppendLine($"`{commandPrefix}deepfry [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}oilpaint [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}twirl [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}pet [emote|user|image] [?speed]`");
        sb.AppendLine($"`{commandPrefix}dallify [emote|user|image]`");
        sb.AppendLine($"`{commandPrefix}dalle [text]`");

        sb.AppendLine();
        sb.AppendLine($"Command arguments:");
        sb.AppendLine($"`user           .. Discord username with # identifier or @mention`");
        sb.AppendLine($"`emote          .. Discord emote (server emotes only)`");
        sb.AppendLine($"`image          .. Attached or linked image in current message.`");
        sb.AppendLine($"`                  If message contains no images,`");
        sb.AppendLine($"`                  reply-to message is checked.`");
        sb.AppendLine($"`speed          .. Petting speed`");
        sb.AppendLine($"`                  Valid speeds: \"slow\", \"normal\",`");
        sb.AppendLine($"`                                \"fast\", \"lightspeed\"`");

        sb.AppendLine();
        sb.AppendLine($"Usage examples:");
        sb.AppendLine($"`{commandPrefix}deepfry :a_server_emote:`");
        sb.AppendLine($"`{commandPrefix}pet @someUser fast`");
        sb.AppendLine($"`{commandPrefix}dallify <message_with_image>`");

        var eb = EmbedBuilderHelper.BuildSimpleEmbed("Stuff you can do with images", sb.ToString());

        return ctx.RespondAsync(eb);
    }
}
