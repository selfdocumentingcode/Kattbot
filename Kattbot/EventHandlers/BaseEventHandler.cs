using DSharpPlus.Entities;

namespace Kattbot.EventHandlers;

public abstract class BaseEventHandler
{
    /// <summary>
    ///     Don't care about about private messages.
    /// </summary>
    protected bool IsPrivateMessageChannel(DiscordChannel channel)
    {
        if (channel.IsPrivate)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Don't care about bot message
    ///     Don't care about non user messages.
    /// </summary>
    protected bool IsReleventMessage(DiscordMessage message)
    {
        // This seems to happen because of the newly introduced Threads feature
        if (message.Author == null)
        {
            return false;
        }

        // Only care about messages from users
        if (message.Author.IsBot || (message.Author.IsSystem ?? false))
        {
            return false;
        }

        return true;
    }

    ///// <summary>
    ///// Don't care about bot reactions
    ///// </summary>
    protected bool IsRelevantReaction(DiscordUser author)
    {
        if (author.IsBot || (author.IsSystem ?? false))
        {
            return false;
        }

        return true;
    }
}
