using Hangfire;
using Hangfire.PostgreSql;
using Malayisha.Application.Abstractions.Jobs;
using Malayisha.Infrastructure.Jobs;
using Malayisha.Infrastructure.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddHangfireJobs(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));

        var hangfireOptions = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
            ?? new HangfireOptions();

        if (!hangfireOptions.Enabled)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("Malayisha")
            ?? throw new InvalidOperationException(
                "Connection string 'Malayisha' was not found. Configure it under ConnectionStrings:Malayisha.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = hangfireOptions.SchemaName,
                    PrepareSchemaIfNecessary = true
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(1, hangfireOptions.WorkerCount);
        });

        services.AddScoped<IAutoCompleteExpiredBookingsJob, AutoCompleteExpiredBookingsJob>();
        services.AddScoped<IExpireBoostsJob, ExpireBoostsJob>();
        services.AddScoped<IRetryFailedNotificationsJob, RetryFailedNotificationsJob>();
    }

    public static WebApplication UseHangfireJobs(this WebApplication app)
    {
        var hangfireOptions = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        if (!hangfireOptions.Enabled)
        {
            return app;
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard(
                hangfireOptions.DashboardPath,
                new DashboardOptions
                {
                    Authorization = [new HangfireDashboardAuthorizationFilter()]
                });
        }

        RecurringJob.AddOrUpdate<IAutoCompleteExpiredBookingsJob>(
            HangfireJobIds.AutoCompleteExpiredBookings,
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *");

        RecurringJob.AddOrUpdate<IExpireBoostsJob>(
            HangfireJobIds.ExpireBoosts,
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<IRetryFailedNotificationsJob>(
            HangfireJobIds.RetryFailedNotifications,
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *");

        return app;
    }
}
