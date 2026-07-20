using Malayisha.Domain.Enums;
using BookingEntity = Malayisha.Domain.Entities.Booking;

namespace Malayisha.Application.Features.Booking.Notifications;

public interface IBookingNotificationDispatcher
{
    Task DispatchStatusChangeAsync(
        BookingEntity booking,
        BookingStatus newStatus,
        Guid actorId,
        bool isSystemAction,
        CancellationToken cancellationToken = default);
}
