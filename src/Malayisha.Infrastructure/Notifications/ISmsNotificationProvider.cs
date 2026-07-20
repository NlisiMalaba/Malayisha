namespace Malayisha.Infrastructure.Notifications;

internal interface ISmsNotificationProvider
{
    Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);
}
