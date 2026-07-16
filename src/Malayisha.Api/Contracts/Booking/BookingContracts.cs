namespace Malayisha.Api.Contracts.Booking;

public sealed record CreateBookingRequest(
    Guid TripListingId,
    Guid? DeliveryRequestId,
    string? Message);

public sealed record QuoteBookingRequest(decimal QuotedPriceZar);

public sealed record ConfirmBookingRequest(decimal AgreedPriceZar);

public sealed record BookingCreatedResponse(Guid BookingId);
