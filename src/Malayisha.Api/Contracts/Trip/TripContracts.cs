namespace Malayisha.Api.Contracts.Trip;

public sealed record CreateTripRequest(
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    string? Description = null);

public sealed record UpdateTripRequest(
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    string? Description = null);

public sealed record SearchTripsRequest(
    string OriginCity,
    string DestinationCity,
    DateOnly? DepartureDate = null,
    decimal? MaxPriceZar = null,
    bool VerifiedOnly = false,
    int Page = 1,
    int PageSize = 20);

public sealed record TripListingDto(
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

public sealed record TripSearchTransporterDto(
    Guid Id,
    string DisplayName,
    bool IsVerified,
    decimal AverageRating,
    string? ProfilePhotoUrl);

public sealed record TripSearchItemDto(
    Guid Id,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal AvailableCapacityKg,
    decimal PriceGuideZar,
    bool IsBoosted,
    TripSearchTransporterDto Transporter);

public sealed record TripSearchPageDto(
    IReadOnlyList<TripSearchItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ShareLinkDto(string Url);
