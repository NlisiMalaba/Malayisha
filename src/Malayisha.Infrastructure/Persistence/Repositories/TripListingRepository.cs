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

    public async Task<TripSearchPage> SearchAsync(
        TripSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var originCity = criteria.OriginCity.Trim().ToLowerInvariant();
        var destinationCity = criteria.DestinationCity.Trim().ToLowerInvariant();
        var todayStartUtc = DateTime.SpecifyKind(criteria.NowUtc.Date, DateTimeKind.Utc);

        var query =
            from trip in dbContext.TripListings.AsNoTracking()
            join profile in dbContext.TransporterProfiles.AsNoTracking()
                on trip.TransporterProfileId equals profile.Id
            join user in dbContext.Users.AsNoTracking()
                on profile.UserId equals user.Id
            where !trip.IsDeleted
                && user.IsActive
                && trip.DepartureDateUtc >= todayStartUtc
                && trip.OriginCity.ToLower() == originCity
                && trip.DestinationCity.ToLower() == destinationCity
            select new { trip, profile };

        if (criteria.DepartureDate is { } departureDate)
        {
            var dayStartUtc = DateTime.SpecifyKind(departureDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);
            query = query.Where(row =>
                row.trip.DepartureDateUtc >= dayStartUtc && row.trip.DepartureDateUtc < dayEndUtc);
        }

        if (criteria.MaxPriceZar is { } maxPriceZar)
        {
            query = query.Where(row => row.trip.PriceGuideZar <= maxPriceZar);
        }

        if (criteria.VerifiedOnly)
        {
            query = query.Where(row => row.profile.IsVerified);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(row => row.trip.IsBoosted)
            .ThenByDescending(row => row.profile.IsVerified)
            .ThenBy(row => row.trip.DepartureDateUtc)
            .ThenBy(row => row.trip.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new TripSearchHit(row.trip, row.profile))
            .ToArray();

        return new TripSearchPage(items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
