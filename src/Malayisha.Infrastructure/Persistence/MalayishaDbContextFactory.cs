using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Malayisha.Infrastructure.Persistence;

public sealed class MalayishaDbContextFactory : IDesignTimeDbContextFactory<MalayishaDbContext>
{
    public MalayishaDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Malayisha")
            ?? Environment.GetEnvironmentVariable("MALAYISHA_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=malayisha;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<MalayishaDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName);
        }).UseSnakeCaseNamingConvention();

        return new MalayishaDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "../Malayisha.Api"),
            Path.Combine(Directory.GetCurrentDirectory(), "src/Malayisha.Api"),
            Path.Combine(AppContext.BaseDirectory, "../../../../../Malayisha.Api")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../Malayisha.Api"));
    }
}
