using Malayisha.Domain.Entities;

namespace Malayisha.Application.Features.Verification;

internal static class VerificationMappings
{
    public static VerificationResponse ToResponse(Domain.Entities.Verification verification) =>
        new(
            verification.Id,
            verification.TransporterProfileId,
            verification.Status,
            verification.SubmittedAtUtc,
            verification.ReviewedByAdminUserId,
            verification.ReviewedAtUtc,
            verification.RejectionReason);

    public static PendingVerificationResponse ToPendingResponse(Domain.Entities.Verification verification) =>
        new(verification.Id, verification.TransporterProfileId, verification.SubmittedAtUtc);
}
