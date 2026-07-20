using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Booking.Notifications;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking;

public interface IBookingTransitionService
{
    Task<Result> ExecuteAsync(
        Guid bookingId,
        Guid actorId,
        UserRole actorRole,
        BookingStatus targetStatus,
        decimal? amountZar,
        bool isSystemAction,
        CancellationToken cancellationToken = default);
}

internal sealed class BookingTransitionService(
    IBookingRepository bookingRepository,
    IBookingNotificationDispatcher bookingNotificationDispatcher,
    TimeProvider timeProvider,
    ILogger<BookingTransitionService> logger) : IBookingTransitionService
{
    public async Task<Result> ExecuteAsync(
        Guid bookingId,
        Guid actorId,
        UserRole actorRole,
        BookingStatus targetStatus,
        decimal? amountZar,
        bool isSystemAction,
        CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.FindByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Error(BookingErrorCodes.BookingNotFound);
        }

        var result = booking.Transition(
            targetStatus,
            actorId,
            actorRole,
            timeProvider.GetUtcNow().UtcDateTime,
            amountZar,
            isSystemAction);

        if (result.IsError)
        {
            return result;
        }

        await bookingRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Booking {BookingId} transitioned to {TargetStatus} by {ActorRole} {ActorId}",
            booking.Id,
            targetStatus,
            actorRole,
            actorId);

        await bookingNotificationDispatcher.DispatchStatusChangeAsync(
            booking,
            targetStatus,
            actorId,
            isSystemAction,
            cancellationToken);

        return Result.Success();
    }
}
