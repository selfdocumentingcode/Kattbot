using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Helpers;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
[Group("utils")]
public class UtilsModule : BaseCommandModule
{
    [Command("emoji-code")]
    public Task GetEmojiCode(CommandContext ctx, DiscordEmoji emoji)
    {
        bool isUnicodeEmoji = emoji.Id == 0;

        if (isUnicodeEmoji)
        {
            var unicodeEncoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);

            byte[] bytes = unicodeEncoding.GetBytes(emoji.Name);

            var sb = new StringBuilder();
            for (var i = 0; i < bytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", bytes[i]);
            }

            var bytesAsString = sb.ToString();

            var formattedSb = new StringBuilder();

            for (var i = 0; i < sb.Length; i += 4)
            {
                formattedSb.Append($"\\u{bytesAsString.Substring(i, length: 4)}");
            }

            var result = formattedSb.ToString();

            return ctx.RespondAsync($"`{result}`");
        }
        else
        {
            string result = EmoteHelper.BuildEmoteCode(emoji.Id, emoji.Name, emoji.IsAnimated);

            return ctx.RespondAsync($"`{result}`");
        }
    }

    [Command("role-id")]
    public Task GetRoleId(CommandContext ctx, string roleName)
    {
        TryResolveResult result = DiscordResolver.TryResolveRoleByName(
            ctx.Guild,
            roleName,
            out DiscordRole? discordRole);

        return !result.Resolved
            ? ctx.RespondAsync(result.ErrorMessage)
            : ctx.RespondAsync($"Role {roleName} has id {discordRole.Id}");
    }

    [Command("avatar-url")]
    public Task GetAvatarUrl(CommandContext ctx, DiscordMember member)
    {
        string memberAvatarUrl = member.AvatarUrl;
        string memberGuildAvatarUrl = member.GuildAvatarUrl;

        var response = $"User avatar: {memberAvatarUrl}";
        if (!string.IsNullOrEmpty(memberGuildAvatarUrl))
            response += $"\nServer avatar: {memberGuildAvatarUrl}";

        return ctx.RespondAsync(response);
    }
}
