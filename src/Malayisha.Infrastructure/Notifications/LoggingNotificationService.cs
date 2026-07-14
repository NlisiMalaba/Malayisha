using Malayisha.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    : INotificationService
{
    public Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "SMS stub dispatched to phone ending {PhoneSuffix}. Message length: {MessageLength}",
            phoneNumber[^4..],
            message.Length);

        return Task.CompletedTask;
    }
}
