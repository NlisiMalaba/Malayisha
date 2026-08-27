using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class BookingRepository(MalayishaDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        dbContext.Bookings.FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListActiveByParticipantAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Bookings
            .Where(booking =>
                (booking.SenderId == userId || booking.TransporterId == userId)
                && booking.Status != BookingStatus.Completed
                && booking.Status != BookingStatus.Cancelled)
            .ToListAsync(cancellationToken);

    public async Task<BookingListPage> ListByParticipantAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.SenderId == userId || booking.TransporterId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(booking => booking.UpdatedAtUtc)
            .ThenByDescending(booking => booking.CreatedAtUtc)
            .ThenBy(booking => booking.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new BookingListPage(items, totalCount);
    }

    public async Task<IReadOnlyList<Booking>> ListDeliveredBeforeAsync(
        DateTime deliveredBeforeUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Delivered
                && booking.DeliveredAtUtc.HasValue
                && booking.DeliveredAtUtc.Value <= deliveredBeforeUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
