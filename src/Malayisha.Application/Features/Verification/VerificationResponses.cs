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

public sealed record PendingVerificationResponse(
    Guid Id,
    Guid TransporterProfileId,
    DateTime SubmittedAtUtc);
