namespace Malayisha.Domain.Common;

public static class NotificationRetryPolicy
{
    public const int MaxAttempts = 3;

    public static readonly TimeSpan[] BackoffIntervals =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    public static DateTime ComputeNextRetryAtUtc(int attemptCount, DateTime failedAtUtc)
    {
        var intervalIndex = Math.Clamp(attemptCount - 1, 0, BackoffIntervals.Length - 1);
        return failedAtUtc.Add(BackoffIntervals[intervalIndex]);
    }
}
