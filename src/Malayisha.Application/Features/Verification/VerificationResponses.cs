using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Verification;

public sealed record VerificationResponse(
    Guid Id,
    Guid TransporterProfileId,
    VerificationStatus Status,
    DateTime SubmittedAtUtc,
    Guid? ReviewedByAdminUserId,
    DateTime? ReviewedAtUtc,
    string? RejectionReason);

public sealed record PendingVerificationProfileResponse(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl,
    bool IsVerified,
    decimal AverageRating);

public sealed record PendingVerificationResponse(
    Guid Id,
    DateTime SubmittedAtUtc,
    PendingVerificationProfileResponse Profile);
