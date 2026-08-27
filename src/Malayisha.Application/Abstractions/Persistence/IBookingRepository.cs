using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public sealed record BookingListPage(
    IReadOnlyList<Booking> Items,
    int TotalCount);

public interface IBookingRepository
{
    Task<Booking?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListActiveByParticipantAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<BookingListPage> ListByParticipantAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> ListDeliveredBeforeAsync(
        DateTime deliveredBeforeUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
