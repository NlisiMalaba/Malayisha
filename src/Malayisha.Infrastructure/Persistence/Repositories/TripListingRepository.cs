using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class TripListingRepository(MalayishaDbContext dbContext) : ITripListingRepository
{
    private static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.Confirmed,
        BookingStatus.InTransit,
        BookingStatus.Delivered
    ];

    public Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
        dbContext.TripListings
            .FirstOrDefaultAsync(trip => trip.Id == tripListingId, cancellationToken);

    public async Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default)
    {
        await dbContext.TripListings.AddAsync(tripListing, cancellationToken);
    }

    public Task<bool> HasBlockingBookingsAsync(
        Guid tripListingId,
        CancellationToken cancellationToken = default) =>
        dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                booking => booking.TripListingId == tripListingId
                    && BlockingStatuses.Contains(booking.Status),
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
