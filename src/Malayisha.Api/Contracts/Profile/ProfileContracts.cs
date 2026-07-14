namespace Malayisha.Api.Contracts.Profile;

public sealed record CreateProfileRequest(
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl = null);

public sealed record UpdateProfileRequest(
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg);

public sealed record UploadProfilePhotoRequest(string FileName, string ContentType);

public sealed record TransporterProfileDto(
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

public sealed record PublicTransporterProfileDto(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl,
    bool IsVerified,
    decimal AverageRating);

public sealed record UploadProfilePhotoDto(
    Uri UploadUrl,
    string ObjectKey,
    Uri CdnUrl,
    DateTime ExpiresAtUtc,
    string ProfilePhotoUrl);
