namespace Malayisha.Application.Features.Trip;

public sealed record TripListingResponse(
    Guid Id,
    Guid TransporterProfileId,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    string? Description,
    bool IsDeleted,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
