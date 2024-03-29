using System.Threading.Tasks;
using Kattbot.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Kattbot.Data.Repositories;

public class GuildSettingsRepository
{
    private readonly KattbotContext _dbContext;

    public GuildSettingsRepository(KattbotContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetGuildSetting(ulong guildId, string key)
    {
        GuildSetting? setting = await _dbContext.GuildSettings
            .SingleOrDefaultAsync(gs => gs.GuildId == guildId && gs.Key == key);

        return setting?.Value;
    }

    public async Task<GuildSetting> SaveGuildSetting(ulong guildId, string key, string value)
    {
        GuildSetting? setting = await _dbContext.GuildSettings
            .SingleOrDefaultAsync(gs => gs.GuildId == guildId && gs.Key == key);

        if (setting == null)
        {
            setting = new GuildSetting
            {
                GuildId = guildId,
                Key = key,
                Value = value,
            };

            await _dbContext.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
        }

        await _dbContext.SaveChangesAsync();

        return setting;
    }
}
