using Malayisha.Domain.Enums;

namespace Malayisha.Api.Contracts.Booking;

public sealed record CreateBookingRequest(
    Guid TripListingId,
    Guid? DeliveryRequestId,
    string? Message);

public sealed record QuoteBookingRequest(decimal QuotedPriceZar);

public sealed record ConfirmBookingRequest(decimal AgreedPriceZar);

public sealed record BookingCreatedResponse(Guid BookingId);

public sealed record ListBookingsRequest(
    int Page = 1,
    int PageSize = 20);

public sealed record BookingDto(
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

public sealed record BookingPageDto(
    IReadOnlyList<BookingDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
