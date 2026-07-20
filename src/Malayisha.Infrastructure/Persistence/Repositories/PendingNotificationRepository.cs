using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class PendingNotificationRepository(MalayishaDbContext dbContext) : IPendingNotificationRepository
{
    public async Task AddAsync(PendingNotification notification, CancellationToken cancellationToken = default)
    {
        await dbContext.PendingNotifications.AddAsync(notification, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingNotification>> ListDueForRetryAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.PendingNotifications
            .Where(notification =>
                notification.NextRetryAtUtc <= nowUtc &&
                notification.AttemptCount < NotificationRetryPolicy.MaxAttempts)
            .OrderBy(notification => notification.NextRetryAtUtc)
            .ToListAsync(cancellationToken);

    public Task RemoveAsync(PendingNotification notification, CancellationToken cancellationToken = default)
    {
        dbContext.PendingNotifications.Remove(notification);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
