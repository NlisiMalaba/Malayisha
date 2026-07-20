using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging;
using BookingEntity = Malayisha.Domain.Entities.Booking;

namespace Malayisha.Application.Features.Booking.Notifications;

internal sealed class BookingNotificationDispatcher(
    IAuthRepository authRepository,
    INotificationService notificationService,
    ILogger<BookingNotificationDispatcher> logger) : IBookingNotificationDispatcher
{
    public async Task DispatchStatusChangeAsync(
        BookingEntity booking,
        BookingStatus newStatus,
        Guid actorId,
        bool isSystemAction,
        CancellationToken cancellationToken = default)
    {
        var recipientUserId = BookingNotificationRecipientResolver.ResolveRecipientUserId(
            booking,
            newStatus,
            actorId,
            isSystemAction);

        var recipient = await authRepository.FindUserByIdAsync(recipientUserId, cancellationToken);
        if (recipient is null)
        {
            logger.LogWarning(
                "Skipping booking push for status {Status}; recipient user {UserId} was not found",
                newStatus,
                recipientUserId);
            return;
        }

        if (string.IsNullOrWhiteSpace(recipient.PushDeviceToken))
        {
            logger.LogDebug(
                "Skipping booking push for status {Status}; recipient user {UserId} has no device token",
                newStatus,
                recipientUserId);
            return;
        }

        var (title, body) = BookingNotificationTemplates.ForStatus(booking, newStatus);

        await notificationService.SendPushAsync(
            recipientUserId,
            recipient.PushDeviceToken,
            title,
            body,
            NotificationKind.Transactional,
            new Dictionary<string, string>
            {
                ["bookingId"] = booking.Id.ToString(),
                ["status"] = newStatus.ToString()
            },
            cancellationToken);
    }
}
