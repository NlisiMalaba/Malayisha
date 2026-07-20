namespace Malayisha.Application.Features.Review;

public sealed record ReviewDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    int Rating,
    string? Comment,
    DateTime CreatedAtUtc);
