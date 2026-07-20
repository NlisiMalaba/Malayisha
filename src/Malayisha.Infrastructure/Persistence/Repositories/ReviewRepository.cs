using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class ReviewRepository(MalayishaDbContext dbContext) : IReviewRepository
{
    public Task<bool> ExistsByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        dbContext.Reviews
            .AsNoTracking()
            .AnyAsync(review => review.BookingId == bookingId, cancellationToken);

    public async Task<IReadOnlyList<int>> ListPublicRatingsByTransporterProfileIdAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.TransporterProfileId == transporterProfileId && !review.IsHidden)
            .Select(review => review.Rating)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> ListPublicByTransporterProfileIdAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.TransporterProfileId == transporterProfileId && !review.IsHidden)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await dbContext.Reviews.AddAsync(review, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
