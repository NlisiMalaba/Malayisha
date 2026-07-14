using Malayisha.Application.Abstractions.Persistence;
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

    public static TripSearchItemResponse ToSearchItem(TripSearchHit hit) =>
        new(
            hit.Trip.Id,
            hit.Trip.OriginCity,
            hit.Trip.DestinationCity,
            hit.Trip.DepartureDateUtc,
            hit.Trip.AvailableCapacityKg,
            hit.Trip.PriceGuideZar,
            hit.Trip.IsBoosted,
            new TripSearchTransporterResponse(
                hit.Transporter.Id,
                hit.Transporter.DisplayName,
                hit.Transporter.IsVerified,
                hit.Transporter.AverageRating,
                hit.Transporter.ProfilePhotoUrl));

    public static TripSearchPageResponse ToSearchPage(
        TripSearchPage page,
        int pageNumber,
        int pageSize) =>
        new(
            page.Items.Select(ToSearchItem).ToArray(),
            pageNumber,
            pageSize,
            page.TotalCount);
}
