using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Helpers
{
    public static class DiscordExtensions
    {
        public static string GetNicknameOrUsername(this DiscordUser user)
        {
            var username = user.Username;

            if(user is DiscordMember)
            {
                var member = (DiscordMember)user;

                username = !string.IsNullOrWhiteSpace(member.Nickname)
                    ? member.Nickname
                    : member.DisplayName;
            }
   
            return username;
        }
    }
}
