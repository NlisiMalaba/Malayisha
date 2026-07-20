using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IPendingNotificationRepository
{
    Task AddAsync(PendingNotification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingNotification>> ListDueForRetryAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(PendingNotification notification, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
