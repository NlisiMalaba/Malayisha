using Malayisha.Application.Abstractions.Persistence;

namespace Malayisha.Application.Features.Booking;

internal static class BookingMappings
{
    public static BookingResponse ToResponse(Domain.Entities.Booking booking) =>
        new(
            booking.Id,
            booking.TripListingId,
            booking.DeliveryRequestId,
            booking.SenderId,
            booking.TransporterId,
            booking.Status,
            booking.QuotedPriceZar,
            booking.AgreedPriceZar,
            booking.Message,
            booking.InTransitAtUtc,
            booking.DeliveredAtUtc,
            booking.CompletedAtUtc,
            booking.CancelledAtUtc,
            booking.CancelledByUserId,
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc);

    public static BookingPageResponse ToPage(
        BookingListPage page,
        int pageNumber,
        int pageSize) =>
        new(
            page.Items.Select(ToResponse).ToArray(),
            pageNumber,
            pageSize,
            page.TotalCount);
}
