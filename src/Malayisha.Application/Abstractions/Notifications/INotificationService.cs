namespace Malayisha.Application.Abstractions.Notifications;

public interface INotificationService
{
    Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);
}
