using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Booking.CreateBooking;

public sealed record CreateBookingCommand(
    Guid SenderId,
    Guid TripListingId,
    Guid? DeliveryRequestId,
    string? Message) : IRequest<Result<Guid>>;
