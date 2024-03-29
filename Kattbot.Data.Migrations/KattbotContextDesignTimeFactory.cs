using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kattbot.Data.Migrations;

public class KattbotContextDesignTimeFactory : IDesignTimeDbContextFactory<KattbotContext>
{
    public KattbotContext CreateDbContext(string[] args)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    $"../{typeof(KattbotContextDesignTimeFactory).Namespace}"))
            .AddJsonFile("appsettings.json", false)
            .AddUserSecrets(typeof(KattbotContextDesignTimeFactory).Assembly)
            .Build();

        var builder = new DbContextOptionsBuilder<KattbotContext>();

        string? dbConnString = config["Kattbot:ConnectionString"];

        builder.UseNpgsql(
            dbConnString,
            options => { options.MigrationsAssembly(typeof(KattbotContextDesignTimeFactory).Assembly.FullName); });

        return new KattbotContext(builder.Options);
    }
}
