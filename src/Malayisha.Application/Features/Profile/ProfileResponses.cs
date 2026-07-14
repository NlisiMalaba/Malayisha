namespace Malayisha.Application.Features.Profile;

public sealed record TransporterProfileResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl,
    bool IsVerified,
    decimal AverageRating,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PublicTransporterProfileResponse(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl,
    bool IsVerified,
    decimal AverageRating);

public sealed record UploadProfilePhotoResponse(
    Uri UploadUrl,
    string ObjectKey,
    Uri CdnUrl,
    DateTime ExpiresAtUtc,
    string ProfilePhotoUrl);
