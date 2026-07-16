namespace Malayisha.Application.Abstractions.Persistence;

public interface ICommissionRecordRepository
{
    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(Domain.Entities.CommissionRecord commissionRecord, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
