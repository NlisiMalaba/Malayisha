using Malayisha.Domain.Enums;
using BookingEntity = Malayisha.Domain.Entities.Booking;

namespace Malayisha.Application.Features.Booking.Notifications;

internal static class BookingNotificationTemplates
{
    public static (string Title, string Body) ForStatus(BookingEntity booking, BookingStatus status) =>
        status switch
        {
            BookingStatus.Quoted => (
                "New quote received",
                $"You received a quote of ZAR {booking.QuotedPriceZar:F2} for your booking."),
            BookingStatus.Confirmed => (
                "Booking confirmed",
                $"A sender confirmed the booking at ZAR {booking.AgreedPriceZar:F2}."),
            BookingStatus.InTransit => (
                "Goods in transit",
                "Your goods are now in transit to the destination."),
            BookingStatus.Delivered => (
                "Goods delivered",
                "Your goods have been delivered."),
            BookingStatus.Completed => (
                "Booking completed",
                "Your booking has been completed."),
            BookingStatus.Cancelled => (
                "Booking cancelled",
                "Your booking has been cancelled."),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported booking notification status.")
        };
}
