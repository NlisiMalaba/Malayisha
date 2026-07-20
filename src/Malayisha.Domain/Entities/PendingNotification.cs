using Malayisha.Domain.Common;

namespace Malayisha.Domain.Entities;

public sealed class PendingNotification
{
    private PendingNotification() { }

    private PendingNotification(
        Guid id,
        Guid userId,
        string deviceToken,
        string title,
        string body,
        string? dataJson,
        int attemptCount,
        DateTime nextRetryAtUtc,
        DateTime createdAtUtc,
        DateTime lastAttemptAtUtc,
        string? lastError)
    {
        Id = id;
        UserId = userId;
        DeviceToken = DomainGuard.Required(deviceToken, nameof(deviceToken));
        Title = DomainGuard.Required(title, nameof(title));
        Body = DomainGuard.Required(body, nameof(body));
        DataJson = dataJson;
        AttemptCount = attemptCount;
        NextRetryAtUtc = nextRetryAtUtc;
        CreatedAtUtc = createdAtUtc;
        LastAttemptAtUtc = lastAttemptAtUtc;
        LastError = lastError;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DeviceToken { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? DataJson { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime NextRetryAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public bool IsExhausted => AttemptCount >= NotificationRetryPolicy.MaxAttempts;

    public static PendingNotification CreateFromFailedPush(
        Guid id,
        Guid userId,
        string deviceToken,
        string title,
        string body,
        string? dataJson,
        DateTime failedAtUtc,
        string? errorMessage) =>
        new(
            id,
            userId,
            deviceToken,
            title,
            body,
            dataJson,
            attemptCount: 1,
            nextRetryAtUtc: NotificationRetryPolicy.ComputeNextRetryAtUtc(1, failedAtUtc),
            createdAtUtc: failedAtUtc,
            lastAttemptAtUtc: failedAtUtc,
            lastError: errorMessage);

    public void RecordRetryFailure(DateTime failedAtUtc, string? errorMessage)
    {
        AttemptCount++;
        LastAttemptAtUtc = failedAtUtc;
        LastError = errorMessage;
        NextRetryAtUtc = NotificationRetryPolicy.ComputeNextRetryAtUtc(AttemptCount, failedAtUtc);
    }
}
