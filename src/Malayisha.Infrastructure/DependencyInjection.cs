using Malayisha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Malayisha")
            ?? throw new InvalidOperationException(
                "Connection string 'Malayisha' was not found. Configure it under ConnectionStrings:Malayisha.");

        services.AddDbContext<MalayishaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName);
            }).UseSnakeCaseNamingConvention());

        return services;
    }
}
