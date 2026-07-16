using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking;

internal static class BookingTransitionExecutor
{
    public static async Task<Result> ExecuteAsync(
        IBookingRepository bookingRepository,
        TimeProvider timeProvider,
        ILogger logger,
        Guid bookingId,
        Guid actorId,
        UserRole actorRole,
        BookingStatus targetStatus,
        decimal? amountZar,
        bool isSystemAction,
        CancellationToken cancellationToken)
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

        return Result.Success();
    }
}
