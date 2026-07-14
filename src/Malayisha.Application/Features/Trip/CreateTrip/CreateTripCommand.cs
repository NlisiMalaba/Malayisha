using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Trip.CreateTrip;

public sealed record CreateTripCommand(
    Guid UserId,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    string? Description = null) : IRequest<Result<TripListingResponse>>;
