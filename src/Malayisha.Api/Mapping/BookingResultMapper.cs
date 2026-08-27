using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Booking;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class BookingResultMapper
{
    public static IActionResult ToCreatedResult(Result<Guid> result) =>
        result.IsSuccess && result.Value != Guid.Empty
            ? new ObjectResult(new BookingCreatedResponse(result.Value))
            {
                StatusCode = StatusCodes.Status201Created
            }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToActionResult(Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToBookingResult(Result<BookingResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToListResult(Result<BookingPageResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToPageDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static BookingDto ToDto(BookingResponse booking) =>
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

    private static BookingPageDto ToPageDto(BookingPageResponse page) =>
        new(
            page.Items.Select(ToDto).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = BookingErrorMapper.ToStatusCode(errorCode)
        };
}
