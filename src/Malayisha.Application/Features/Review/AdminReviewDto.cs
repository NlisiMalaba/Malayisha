namespace Malayisha.Application.Features.Review;

public sealed record AdminReviewDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    Guid TransporterProfileId,
    int Rating,
    string? Comment,
    bool IsHidden,
    DateTime CreatedAtUtc);
