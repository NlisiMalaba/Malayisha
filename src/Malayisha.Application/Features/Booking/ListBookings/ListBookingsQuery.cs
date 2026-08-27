using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Booking.ListBookings;

public sealed record ListBookingsQuery(
    Guid UserId,
    int Page = BookingValidation.DefaultPage,
    int PageSize = BookingValidation.DefaultPageSize) : IRequest<Result<BookingPageResponse>>;
