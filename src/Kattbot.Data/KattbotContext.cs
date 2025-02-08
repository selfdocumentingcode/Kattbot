using Kattbot.Common.Models;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Common.Models.Emotes;
using Microsoft.EntityFrameworkCore;

namespace Kattbot.Data;

public class KattbotContext : DbContext
{
    public KattbotContext(DbContextOptions options)
        : base(options)
    { }

    public DbSet<EmoteEntity> Emotes { get; set; } = null!;

    public DbSet<BotUserRole> BotUserRoles { get; set; } = null!;

    public DbSet<GuildSetting> GuildSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<BotUserRole>()
            .HasIndex(ur => new { ur.UserId, ur.BotRoleType })
            .IsUnique();

        builder.Entity<GuildSetting>()
            .HasIndex(ur => new { ur.GuildId, ur.Key })
            .IsUnique();
    }
}
