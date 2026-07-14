using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface ITripListingRepository
{
    Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default);

    Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the trip has at least one booking in Confirmed, InTransit, or Delivered status.
    /// </summary>
    Task<bool> HasBlockingBookingsAsync(Guid tripListingId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
