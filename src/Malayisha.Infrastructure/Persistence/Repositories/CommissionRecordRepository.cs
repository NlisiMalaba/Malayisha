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

    public Task<CommissionRecord?> FindByIdForUpdateAsync(
        Guid commissionRecordId,
        CancellationToken cancellationToken = default) =>
        dbContext.CommissionRecords
            .FirstOrDefaultAsync(record => record.Id == commissionRecordId, cancellationToken);

    public async Task<IReadOnlyList<CommissionRecord>> ListByCriteriaAsync(
        CommissionReportCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CommissionRecords.AsNoTracking().AsQueryable();

        if (criteria.Status.HasValue)
        {
            query = query.Where(record => record.Status == criteria.Status.Value);
        }

        if (criteria.FromCompletionDateUtc.HasValue)
        {
            query = query.Where(record => record.CompletionDateUtc >= criteria.FromCompletionDateUtc.Value);
        }

        if (criteria.ToCompletionDateUtc.HasValue)
        {
            query = query.Where(record => record.CompletionDateUtc <= criteria.ToCompletionDateUtc.Value);
        }

        return await query
            .OrderByDescending(record => record.CompletionDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CommissionRecord commissionRecord, CancellationToken cancellationToken = default)
    {
        await dbContext.CommissionRecords.AddAsync(commissionRecord, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
