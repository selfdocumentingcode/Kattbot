using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Kattbot.Data.Migrations
{
    public class KattbotContextDesignTimeFactory : IDesignTimeDbContextFactory<KattbotContext>
    {
        public KattbotContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                            $"../{typeof(KattbotContextDesignTimeFactory).Namespace}"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets(typeof(KattbotContextDesignTimeFactory).Assembly)
                .Build();

            var builder = new DbContextOptionsBuilder<KattbotContext>();

            var dbConnString = config["Kattbot:ConnectionString"];

            builder.UseNpgsql(dbConnString, options => {
                options.MigrationsAssembly(typeof(KattbotContextDesignTimeFactory).Assembly.FullName);
            });

            return new KattbotContext(builder.Options);
        }
    }
}
