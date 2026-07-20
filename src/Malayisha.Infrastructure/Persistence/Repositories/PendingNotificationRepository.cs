using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class PendingNotificationRepository(MalayishaDbContext dbContext) : IPendingNotificationRepository
{
    public async Task AddAsync(PendingNotification notification, CancellationToken cancellationToken = default)
    {
        await dbContext.PendingNotifications.AddAsync(notification, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
