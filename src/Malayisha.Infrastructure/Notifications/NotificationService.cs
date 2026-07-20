using System.Text.Json;
using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class NotificationService(
    ISmsNotificationProvider smsProvider,
    IFcmPushNotificationSender fcmSender,
    IPendingNotificationRepository pendingNotificationRepository,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    public Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default) =>
        smsProvider.SendSmsAsync(phoneNumber, message, cancellationToken);

    public async Task SendPushAsync(
        Guid userId,
        string deviceToken,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var result = await fcmSender.SendAsync(
            new FcmPushMessage(deviceToken, title, body, data),
            cancellationToken);

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Push notification sent to user {UserId}. FCM message id: {MessageId}",
                userId,
                result.MessageId);

            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var dataJson = data is null || data.Count == 0
            ? null
            : JsonSerializer.Serialize(data);

        var pending = PendingNotification.CreateFromFailedPush(
            Guid.NewGuid(),
            userId,
            deviceToken,
            title,
            body,
            dataJson,
            nowUtc,
            result.ErrorMessage);

        await pendingNotificationRepository.AddAsync(pending, cancellationToken);
        await pendingNotificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Push notification failed for user {UserId}; queued for retry {PendingNotificationId} at {NextRetryAtUtc}. Error: {Error}",
            userId,
            pending.Id,
            pending.NextRetryAtUtc,
            result.ErrorMessage);
    }
}
