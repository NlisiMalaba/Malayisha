using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Trip.UpdateTrip;

public sealed record UpdateTripCommand(
    Guid UserId,
    Guid TripListingId,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    string? Description = null) : IRequest<Result<TripListingResponse>>;
