using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Common.Models.Emotes
{
    public class TempEmote
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = null!;
        public bool Animated { get; set; }
    }
}
