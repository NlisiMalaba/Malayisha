using Malayisha.Domain.Entities;

namespace Malayisha.Application.Features.Trip;

internal static class TripMappings
{
    public static TripListingResponse ToResponse(TripListing trip) =>
        new(
            trip.Id,
            trip.TransporterProfileId,
            trip.OriginCity,
            trip.DestinationCity,
            trip.DepartureDateUtc,
            trip.AvailableCapacityKg,
            trip.PriceGuideZar,
            trip.Description,
            trip.IsDeleted,
            trip.CreatedAtUtc,
            trip.UpdatedAtUtc);
}
