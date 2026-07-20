using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IReviewRepository
{
    Task<bool> ExistsByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListPublicRatingsByTransporterProfileIdAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> ListPublicByTransporterProfileIdAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default);

    Task<Review?> FindByIdForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Review>> ListAllOrderedByCreatedAtDescAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(Review review, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
