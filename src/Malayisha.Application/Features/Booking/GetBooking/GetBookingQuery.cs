using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Booking.GetBooking;

public sealed record GetBookingQuery(Guid UserId, Guid BookingId) : IRequest<Result<BookingResponse>>;
