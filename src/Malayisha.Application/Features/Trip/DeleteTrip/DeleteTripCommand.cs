using Malayisha.Domain.Common;
using MediatR;

namespace Malayisha.Application.Features.Trip.DeleteTrip;

public sealed record DeleteTripCommand(
    Guid UserId,
    Guid TripListingId) : IRequest<Result>;
