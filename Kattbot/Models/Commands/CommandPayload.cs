using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Models
{
    public abstract class CommandPayload
    {
        public EmoteSource Source { get; set; }
        public DiscordGuild Guild { get; set; } = null!;
    }
}
