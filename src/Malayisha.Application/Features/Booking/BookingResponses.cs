using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Booking;

public sealed record BookingResponse(
    Guid Id,
    Guid TripListingId,
    Guid? DeliveryRequestId,
    Guid SenderId,
    Guid TransporterId,
    BookingStatus Status,
    decimal? QuotedPriceZar,
    decimal? AgreedPriceZar,
    string? Message,
    DateTime? InTransitAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    Guid? CancelledByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookingPageResponse(
    IReadOnlyList<BookingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
