using Kattbot.Common.Models.BotRoles;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Data.Repositories
{
    public class BotUserRolesRepository
    {
        private readonly KattbotContext _dbContext;

        public BotUserRolesRepository(KattbotContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> UserHasRole(ulong userId, BotRoleType roleType)
        {
            var role = await _dbContext.BotUserRoles
                .SingleOrDefaultAsync(r => r.UserId == userId && r.BotRoleType == roleType);

            return role != null;
        }

        public async Task AddUserRole(ulong userId, BotRoleType roleType)
        {
            var botUserRole = new BotUserRole()
            {
                UserId = userId,
                BotRoleType = roleType
            };

            await _dbContext.AddAsync(botUserRole);

            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveUserRole(ulong userId, BotRoleType roleType)
        {
            var role = await _dbContext.BotUserRoles
                 .SingleOrDefaultAsync(r => r.UserId == userId && r.BotRoleType == roleType);

            if (role == null)
                throw new Exception("User does not have role");

            _dbContext.Remove(role);

            await _dbContext.SaveChangesAsync();
        }
    }
}
