using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Common.Models
{
    public class GuildSetting
    {
        [Key]
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
