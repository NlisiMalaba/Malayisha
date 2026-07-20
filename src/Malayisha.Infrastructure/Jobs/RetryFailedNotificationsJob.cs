using System.Text.Json;
using Malayisha.Application.Abstractions.Jobs;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class RetryFailedNotificationsJob(
    IPendingNotificationRepository pendingNotificationRepository,
    IFcmPushNotificationSender fcmSender,
    TimeProvider timeProvider,
    ILogger<RetryFailedNotificationsJob> logger) : IRetryFailedNotificationsJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var dueNotifications = await pendingNotificationRepository.ListDueForRetryAsync(nowUtc, cancellationToken);

        if (dueNotifications.Count == 0)
        {
            logger.LogDebug("RetryFailedNotificationsJob found no due notifications.");
            return;
        }

        var succeededCount = 0;
        var rescheduledCount = 0;
        var discardedCount = 0;

        foreach (var pending in dueNotifications)
        {
            var result = await fcmSender.SendAsync(
                new FcmPushMessage(
                    pending.DeviceToken,
                    pending.Title,
                    pending.Body,
                    DeserializeData(pending.DataJson)),
                cancellationToken);

            if (result.Succeeded)
            {
                await pendingNotificationRepository.RemoveAsync(pending, cancellationToken);
                await pendingNotificationRepository.SaveChangesAsync(cancellationToken);
                succeededCount++;

                logger.LogInformation(
                    "Retry succeeded for pending notification {PendingNotificationId} (user {UserId}). FCM message id: {MessageId}",
                    pending.Id,
                    pending.UserId,
                    result.MessageId);

                continue;
            }

            var failedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            pending.RecordRetryFailure(failedAtUtc, result.ErrorMessage);

            if (pending.IsExhausted)
            {
                await pendingNotificationRepository.RemoveAsync(pending, cancellationToken);
                discardedCount++;

                logger.LogWarning(
                    "Discarded pending notification {PendingNotificationId} for user {UserId} after {AttemptCount} failed attempts. Last error: {Error}",
                    pending.Id,
                    pending.UserId,
                    pending.AttemptCount,
                    result.ErrorMessage);
            }
            else
            {
                rescheduledCount++;

                logger.LogWarning(
                    "Retry failed for pending notification {PendingNotificationId}; next attempt at {NextRetryAtUtc}. Error: {Error}",
                    pending.Id,
                    pending.NextRetryAtUtc,
                    result.ErrorMessage);
            }

            await pendingNotificationRepository.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "RetryFailedNotificationsJob processed {DueCount} notifications; succeeded {SucceededCount}, rescheduled {RescheduledCount}, discarded {DiscardedCount}",
            dueNotifications.Count,
            succeededCount,
            rescheduledCount,
            discardedCount);
    }

    private static IReadOnlyDictionary<string, string>? DeserializeData(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson);
    }
}
