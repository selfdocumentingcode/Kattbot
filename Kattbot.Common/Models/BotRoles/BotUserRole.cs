using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Common.Models.BotRoles
{
    public class BotUserRole
    {
        public Guid Id { get; set; }
        public ulong UserId { get; set; }
        public BotRoleType BotRoleType { get; set; }
    }
}
