namespace Malayisha.Application.Features.Review;

internal static class ReviewMappings
{
    internal static ReviewDto ToDto(Domain.Entities.Review review) =>
        new(
            review.Id,
            review.BookingId,
            review.SenderId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc);

    internal static AdminReviewDto ToAdminDto(Domain.Entities.Review review) =>
        new(
            review.Id,
            review.BookingId,
            review.SenderId,
            review.TransporterProfileId,
            review.Rating,
            review.Comment,
            review.IsHidden,
            review.CreatedAtUtc);
}
