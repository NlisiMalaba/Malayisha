namespace Malayisha.Application.Abstractions.Notifications;

public interface INotificationService
{
    Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);

    Task SendPushAsync(
        Guid userId,
        string deviceToken,
        string title,
        string body,
        NotificationKind kind = NotificationKind.Transactional,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
