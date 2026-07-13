using Malayisha.Application.Abstractions.Jobs;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class RetryFailedNotificationsJob(
    ILogger<RetryFailedNotificationsJob> logger) : IRetryFailedNotificationsJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Business logic is implemented in task 13.2 (retry PendingNotifications with backoff).
        logger.LogDebug("RetryFailedNotificationsJob invoked; awaiting notification workflow wiring.");
        return Task.CompletedTask;
    }
}
