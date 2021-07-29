using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.EventHandlers
{
    public abstract class BaseEventHandler
    {
        /// <summary>
        /// Don't care about about private messages
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        protected bool IsPrivateMessageChannel(DiscordChannel channel)
        {
            if (channel.IsPrivate)
                return true;

            return false;
        }

        /// <summary>
        /// Don't care about bot message
        /// Don't care about non user messages
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected bool IsReleventMessage(DiscordMessage message)
        {
            // This seems to happen because of the newly introduced Threads feature
            if (message.Author == null)
                return false;

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
        ///// <param name="reaction"></param>
        ///// <returns></returns>
        protected bool IsRelevantReaction(DiscordUser author)
        {
            if (author.IsBot || (author.IsSystem ?? false))
                return false;

            return true;
        }
    }


}
