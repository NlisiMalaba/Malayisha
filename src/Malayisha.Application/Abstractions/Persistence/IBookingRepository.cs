using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IBookingRepository
{
    Task<Booking?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListDeliveredBeforeAsync(
        DateTime deliveredBeforeUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
