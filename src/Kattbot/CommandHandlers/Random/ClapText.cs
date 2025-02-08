using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using MediatR;

namespace Kattbot.CommandHandlers.Random;

public class ClapTextRequest : CommandRequest
{
    public ClapTextRequest(CommandContext ctx, string text)
        : base(ctx)
    {
        Text = text;
    }

    public string Text { get; }
}

public class ClapTextRequestHandler : IRequestHandler<ClapTextRequest>
{
    public async Task Handle(ClapTextRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string text = request.Text;
        DiscordMessage? reply = ctx.Message.ReferencedMessage;

        if (reply is not null)
        {
            text = reply.Content;
        }

        if (string.IsNullOrEmpty(text))
        {
            await ctx.RespondAsync("What am I supposed to clap?");
            return;
        }

        DiscordEmoji clapEmoji = DiscordEmoji.FromUnicode(EmojiMap.Clap);

        string[] textParts = text.Split(' ');

        string innerClappedMessage = string.Join($" {clapEmoji} ", textParts.Select(x => x.ToUpper()));

        var clappedMessage = $"{clapEmoji} {innerClappedMessage} {clapEmoji}";

        if (clappedMessage.Length > DiscordConstants.MaxMessageLength)
        {
            await ctx.RespondAsync("I can't clap that much!");
            return;
        }

        await ctx.RespondAsync(clappedMessage);
    }
}
