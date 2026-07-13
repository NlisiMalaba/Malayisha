using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Malayisha.Infrastructure.Persistence;

public sealed class MalayishaDbContextFactory : IDesignTimeDbContextFactory<MalayishaDbContext>
{
    public MalayishaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Malayisha.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Malayisha")
            ?? throw new InvalidOperationException(
                "Connection string 'Malayisha' was not found. Configure it in Malayisha.Api/appsettings.json.");

        var optionsBuilder = new DbContextOptionsBuilder<MalayishaDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName);
        }).UseSnakeCaseNamingConvention();

        return new MalayishaDbContext(optionsBuilder.Options);
    }
}
