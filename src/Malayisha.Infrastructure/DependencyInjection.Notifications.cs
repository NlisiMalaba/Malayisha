using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Infrastructure.Notifications;
using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));

        var provider = configuration.GetSection(SmsOptions.SectionName).GetValue<string>(nameof(SmsOptions.Provider))
            ?? "Logging";

        switch (provider.Trim().ToLowerInvariant())
        {
            case "twilio":
                services.AddSingleton<INotificationService, TwilioSmsService>();
                break;
            case "africastalking":
            case "africas-talking":
                services.AddHttpClient(nameof(AfricasTalkingSmsService));
                services.AddSingleton<INotificationService, AfricasTalkingSmsService>();
                break;
            default:
                services.AddSingleton<INotificationService, LoggingNotificationService>();
                break;
        }
    }
}
