using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public sealed record TripSearchCriteria(
    string OriginCity,
    string DestinationCity,
    DateOnly? DepartureDate,
    decimal? MaxPriceZar,
    bool VerifiedOnly,
    DateTime NowUtc,
    int Page,
    int PageSize);

public sealed record TripSearchHit(
    TripListing Trip,
    TransporterProfile Transporter);

public sealed record TripSearchPage(
    IReadOnlyList<TripSearchHit> Items,
    int TotalCount);

public interface ITripListingRepository
{
    Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default);

    Task<TripListing?> FindByIdForUpdateAsync(Guid tripListingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TripListing>> ListExpiredBoostedForUpdateAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the trip has at least one booking in Confirmed, InTransit, or Delivered status.
    /// </summary>
    Task<bool> HasBlockingBookingsAsync(Guid tripListingId, CancellationToken cancellationToken = default);

    Task<TripSearchPage> SearchAsync(
        TripSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
