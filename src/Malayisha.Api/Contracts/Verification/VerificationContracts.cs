using Malayisha.Domain.Enums;

namespace Malayisha.Api.Contracts.Verification;

public sealed record RejectVerificationRequest(string? RejectionReason = null);

public sealed record VerificationDto(
    Guid Id,
    Guid TransporterProfileId,
    VerificationStatus Status,
    DateTime SubmittedAtUtc,
    Guid? ReviewedByAdminUserId,
    DateTime? ReviewedAtUtc,
    string? RejectionReason);

public sealed record PendingVerificationProfileDto(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl,
    bool IsVerified,
    decimal AverageRating);

public sealed record PendingVerificationDto(
    Guid Id,
    DateTime SubmittedAtUtc,
    PendingVerificationProfileDto Profile);
