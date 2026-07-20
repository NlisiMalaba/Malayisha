using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Infrastructure.Notifications;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.Configure<PushOptions>(configuration.GetSection(PushOptions.SectionName));

        AddSmsProvider(services, configuration);
        AddPushProvider(services, configuration);

        services.AddScoped<IPendingNotificationRepository, PendingNotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();
    }

    private static void AddSmsProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(SmsOptions.SectionName).GetValue<string>(nameof(SmsOptions.Provider))
            ?? "Logging";

        switch (provider.Trim().ToLowerInvariant())
        {
            case "twilio":
                services.AddSingleton<ISmsNotificationProvider, TwilioSmsService>();
                break;
            case "africastalking":
            case "africas-talking":
                services.AddHttpClient(nameof(AfricasTalkingSmsService));
                services.AddSingleton<ISmsNotificationProvider, AfricasTalkingSmsService>();
                break;
            default:
                services.AddSingleton<ISmsNotificationProvider, LoggingSmsNotificationProvider>();
                break;
        }
    }

    private static void AddPushProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(PushOptions.SectionName).GetValue<string>(nameof(PushOptions.Provider))
            ?? "Logging";

        switch (provider.Trim().ToLowerInvariant())
        {
            case "fcm":
            case "firebase":
                services.AddSingleton<IFcmPushNotificationSender, FcmPushNotificationSender>();
                break;
            default:
                services.AddSingleton<IFcmPushNotificationSender, LoggingFcmPushNotificationSender>();
                break;
        }
    }
}
