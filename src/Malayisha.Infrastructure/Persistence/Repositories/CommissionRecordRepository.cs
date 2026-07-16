using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class CommissionRecordRepository(MalayishaDbContext dbContext) : ICommissionRecordRepository
{
    public Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        dbContext.CommissionRecords
            .AsNoTracking()
            .AnyAsync(record => record.BookingId == bookingId, cancellationToken);

    public async Task AddAsync(CommissionRecord commissionRecord, CancellationToken cancellationToken = default)
    {
        await dbContext.CommissionRecords.AddAsync(commissionRecord, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
