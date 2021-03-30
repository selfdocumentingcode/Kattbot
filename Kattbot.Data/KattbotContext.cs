using Kattbot.Common.Models;
using Kattbot.Common.Models.BotRoles;
using Kattbot.Common.Models.Emotes;
using Kattbot.Common.Models.Events;
using Microsoft.EntityFrameworkCore;

namespace Kattbot.Data
{
    public class KattbotContext : DbContext
    {
        public KattbotContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<EventTemplate>()
                .HasIndex(et => new { et.GuildId, et.Name })
                .IsUnique();

            builder.Entity<BotUserRole>()
                .HasIndex(ur => new { ur.UserId, ur.BotRoleType })
                .IsUnique();

            builder.Entity<GuildSetting>()
                .HasIndex(ur => new { ur.GuildId, ur.Key })
                .IsUnique();
        }

        public DbSet<EmoteEntity> Emotes { get; set; } = null!;
        public DbSet<EventTemplate> EventTemplates { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventAttendee> EventAttendees { get; set; } = null!;
        public DbSet<BotUserRole> BotUserRoles { get; set; } = null!;
        public DbSet<GuildSetting> GuildSettings { get; set; } = null!;
    }
}
