using DSharpPlus.Entities;

namespace Kattbot.NotificationHandlers;

public abstract class BaseNotificationHandler
{
    /// <summary>
    ///     Don't care about bot message
    ///     Don't care about non-user messages.
    /// </summary>
    protected static bool IsRelevantMessage(DiscordMessage message)
    {
        // This seems to happen because of the newly introduced Threads feature
        if (message.Author is null)
        {
            return false;
        }

        if (message.Channel?.IsPrivate ?? false)
        {
            return false;
        }

        // Only care about messages from users
        return IsRelevantAuthor(message.Author);
    }

    ///// <summary>
    ///// Don't care about bot reactions
    ///// </summary>
    protected static bool IsRelevantAuthor(DiscordUser author)
    {
        return !author.IsBot && !(author.IsSystem ?? false);
    }
}
