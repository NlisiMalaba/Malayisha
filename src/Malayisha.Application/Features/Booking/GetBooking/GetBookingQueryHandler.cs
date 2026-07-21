using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.GetBooking;

internal sealed class GetBookingQueryHandler(
    IBookingRepository bookingRepository,
    ILogger<GetBookingQueryHandler> logger)
    : IRequestHandler<GetBookingQuery, Result<BookingResponse>>
{
    public async Task<Result<BookingResponse>> Handle(
        GetBookingQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.FindByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result<BookingResponse>.Error(BookingErrorCodes.BookingNotFound);
        }

        if (booking.SenderId != request.UserId && booking.TransporterId != request.UserId)
        {
            return Result<BookingResponse>.Error(BookingErrorCodes.NotBookingParticipant);
        }

        logger.LogInformation(
            "Loaded booking {BookingId} for participant {UserId}",
            booking.Id,
            request.UserId);

        return Result<BookingResponse>.Success(BookingMappings.ToResponse(booking));
    }
}
