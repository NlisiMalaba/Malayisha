using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.ListBookings;

internal sealed class ListBookingsQueryHandler(
    IBookingRepository bookingRepository,
    ILogger<ListBookingsQueryHandler> logger)
    : IRequestHandler<ListBookingsQuery, Result<BookingPageResponse>>
{
    public async Task<Result<BookingPageResponse>> Handle(
        ListBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await bookingRepository.ListByParticipantAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = BookingMappings.ToPage(page, request.Page, request.PageSize);

        logger.LogInformation(
            "Listed {Count} of {TotalCount} bookings for user {UserId} (page {Page})",
            response.Items.Count,
            response.TotalCount,
            request.UserId,
            response.Page);

        return Result<BookingPageResponse>.Success(response);
    }
}
