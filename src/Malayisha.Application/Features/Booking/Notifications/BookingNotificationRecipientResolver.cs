using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using BookingEntity = Malayisha.Domain.Entities.Booking;

namespace Malayisha.Application.Features.Booking.Notifications;

internal static class BookingNotificationRecipientResolver
{
    public static Guid ResolveRecipientUserId(
        BookingEntity booking,
        BookingStatus newStatus,
        Guid actorId,
        bool isSystemAction) =>
        newStatus switch
        {
            BookingStatus.Quoted => booking.SenderId,
            BookingStatus.Confirmed => booking.TransporterId,
            BookingStatus.InTransit => booking.SenderId,
            BookingStatus.Delivered => booking.SenderId,
            BookingStatus.Completed when isSystemAction => booking.SenderId,
            BookingStatus.Completed => booking.TransporterId,
            BookingStatus.Cancelled when actorId == booking.SenderId => booking.TransporterId,
            BookingStatus.Cancelled => booking.SenderId,
            _ => throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, "Unsupported booking notification status.")
        };
}
