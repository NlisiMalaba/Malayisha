using Malayisha.Application.Abstractions.Caching;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Infrastructure.Caching;
using Malayisha.Infrastructure.Otp;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPostgreSql(services, configuration);
        AddRedis(services, configuration);
        AddS3(services, configuration);
        AddHangfireJobs(services, configuration);
        return services;
    }

    private static void AddPostgreSql(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Malayisha")
            ?? throw new InvalidOperationException(
                "Connection string 'Malayisha' was not found. Configure it under ConnectionStrings:Malayisha.");

        services.AddDbContext<MalayishaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName);
            }).UseSnakeCaseNamingConvention());
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value;

            if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string was not found. Configure it under Redis:ConnectionString.");
            }

            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IOtpStore, RedisOtpStore>();
    }
}
