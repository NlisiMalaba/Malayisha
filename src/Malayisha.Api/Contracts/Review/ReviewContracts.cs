namespace Malayisha.Api.Contracts.Review;

public sealed record CreateReviewRequest(
    Guid BookingId,
    int Rating,
    string? Comment);

public sealed record ReviewDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    int Rating,
    string? Comment,
    DateTime CreatedAtUtc);

public sealed record TransporterReviewsResponse(IReadOnlyList<ReviewDto> Reviews);
