namespace Malayisha.Application.Features.Trip;

public sealed record BoostedTripDto(
    Guid Id,
    Guid TransporterProfileId,
    bool IsBoosted,
    DateTime? BoostStartAtUtc,
    DateTime? BoostEndAtUtc,
    DateTime UpdatedAtUtc);
