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

public sealed record TripSearchTransporterResponse(
    Guid Id,
    string DisplayName,
    bool IsVerified,
    decimal AverageRating,
    string? ProfilePhotoUrl);

public sealed record TripSearchItemResponse(
    Guid Id,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    bool IsBoosted,
    TripSearchTransporterResponse Transporter);

public sealed record TripSearchPageResponse(
    IReadOnlyList<TripSearchItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
